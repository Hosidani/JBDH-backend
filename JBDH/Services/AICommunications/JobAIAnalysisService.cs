using JBDH.Models;
using JBDH.Models.AI;
using JBDH.Services.Configuration;
using System.Text;
using Microsoft.EntityFrameworkCore;
using JBDH.Services.UserManagement;
using JBDH.Services.Helpers;
using JBDH.Models.Core;
using JBDH.Models.User;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Threading;

namespace JBDH.Services.AICommunications
{
    public class JobAIAnalysisService : IJobAIAnalysisService
    {
        private readonly IUserPropertiesService _userPropertiesService;
        private readonly IAIRequestBuilderService _aiRequestBuilderService;
        private readonly IManageIOService _manageIOService;
        private readonly IAIApiClient _aiApiClient;
        private readonly ITextChunkingService _textChunkingService;
        private readonly ILogger<JobAIAnalysisService> _logger;
        private static readonly AIConfig AIConfig = new AIConfig();
        private readonly ApplicationDbContext _context;
        
        // SemaphoreSlim för att begränsa samtidiga anrop
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly int _maxConcurrency;

        public JobAIAnalysisService(
            IUserPropertiesService userPropertiesService,
            IAIRequestBuilderService aIRequestBuilderService,
            ApplicationDbContext context,
            IManageIOService manageIOService,
            IAIApiClient aiApiClient,
            ITextChunkingService textChunkingService,
            ILogger<JobAIAnalysisService> logger)
        {
            _userPropertiesService = userPropertiesService;
            _aiRequestBuilderService = aIRequestBuilderService;
            _context = context;
            _manageIOService = manageIOService;
            _aiApiClient = aiApiClient;
            _textChunkingService = textChunkingService;
            _logger = logger;
            
            // Begränsa till 3 samtidiga anrop för att inte överbelasta databas/nätverk
            _maxConcurrency = 3;
            _concurrencySemaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        }

        // Polly Retry Policy specifikt för DeepSeek 429-fel
        private static readonly IAsyncPolicy<HttpResponseMessage> DeepSeekRetryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(r => 
                r.StatusCode == HttpStatusCode.TooManyRequests || // 429
                r.StatusCode == HttpStatusCode.ServiceUnavailable || // 503
                r.StatusCode == HttpStatusCode.BadGateway || // 502
                r.StatusCode == HttpStatusCode.InternalServerError) // 500
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(new Random().Next(0, 1000)), // Exponential backoff + jitter
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    if (outcome.Exception != null)
                    {
                        logger?.LogWarning($"Retry {retryCount} for DeepSeek API after {timespan}s due to: {outcome.Exception.Message}");
                    }
                    else if (outcome.Result != null)
                    {
                        logger?.LogWarning($"Retry {retryCount} for DeepSeek API after {timespan}s due to HTTP {(int)outcome.Result.StatusCode} {outcome.Result.StatusCode}");
                    }
                }
            );

        // Old method for getting or creating job analysis based on job ad URL and job ID

        public async Task<string> GetOrCreateAnalysisAsync(string jobAdUrl, int jobId)
        {
            // 1. Check for cached response in the database
            var cachedResponse = await _context.JobAIResponses
                .Where(r => r.JobId == jobId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (cachedResponse != null)
                return cachedResponse.ResponseText;

            // 2. Build and send AI request
            var user = await _userPropertiesService.GetCurrentUserAsync();
            var request = new AIRequest
            {
                CommandId = 1,
                JobAdUrl = jobAdUrl,
                User = user
            };
            var requestContent = await _aiRequestBuilderService.AIRequestBuilder(request);

            using (var httpClient = new HttpClient())
            {
                var apiUrl = AIConfig.OpenAIUrl;
                if (string.IsNullOrEmpty(AIConfig.OpenAIKey))
                    throw new InvalidOperationException("OpenAI API key is not configured.");

                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AIConfig.OpenAIKey}");

                var response = await httpClient.PostAsync(apiUrl, requestContent);

                if (response == null)
                    throw new Exception("HTTP response is null.");

                var responseContent = await response.Content.ReadAsStringAsync();
                if (responseContent == null)
                    throw new Exception("AI response content is null.");

                if (_manageIOService == null)
                    throw new Exception("_manageIOService is null!");

                var cleanJson = _manageIOService.ExtractInnerJsonFromJobAIResponse(responseContent);
                if (cleanJson == null)
                    throw new Exception("Extracted JSON is null.");

                // 3. Save the new response to the database
                var aiResponse = new JobAIResponse
                {
                    JobId = jobId,
                    ResponseText = cleanJson,
                    CreatedAt = DateTime.UtcNow
                };
                _context.JobAIResponses.Add(aiResponse);
                await _context.SaveChangesAsync();

                return cleanJson; 
            }
        }
        
        public async Task<string> GetCvJobComparisonAsync(string jobAdText, string cvPath)
        {
            var request = new AIRequest
            {
                CommandId = 5,
                CVPath = cvPath,
                JobAdText = jobAdText
            };
            var aiResult = await _aiApiClient.GetAIResponse(request); 
            return aiResult?.ToString() ?? string.Empty;
        }

        public async Task<(List<Requirement> requirements, Profession? jobProfession)> ExtractJobRequirementsAndProfessionAsync(string jobAdText)
        {
            if (string.IsNullOrWhiteSpace(jobAdText))
            {
                _logger.LogWarning("Job ad text is empty or null");
                return (new List<Requirement>(), null);
            }

            // Chunk job ad (RAG)
            var chunks = _textChunkingService.ChunkJobAd(jobAdText);

            var requirements = new List<Requirement>();
            Profession? jobProfession = null;

            foreach (var chunk in chunks)
            {
                var aiRequest = new AIRequest
                {
                    CommandId = 8, // requirement extraction
                    JobAdText = chunk,
                    User = new User
                    {
                        Id = 1,
                        Email = "system@analysis",
                        PasswordHash = "system",
                        Location = "system"
                    }
                };

                var aiResult = await _aiApiClient.GetAIResponse(aiRequest);
                if (!aiResult.Success || string.IsNullOrWhiteSpace(aiResult.Response))
                    continue;

                try
                {
                    // Extract JSON from code blocks if needed
                    var cleanResponse = aiResult.Response;
                    if (cleanResponse.Contains("```json"))
                    {
                        var startIndex = cleanResponse.IndexOf("```json") + 7;
                        var endIndex = cleanResponse.LastIndexOf("```");
                        if (endIndex > startIndex)
                            cleanResponse = cleanResponse.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                    
                    var extracted = JsonConvert.DeserializeObject<RequirementExtractionResponse>(cleanResponse);

                    if (extracted?.Requirements != null)
                        requirements.AddRange(extracted.Requirements);
                    
                    // Capture job profession from first successful extraction
                    if (jobProfession == null && extracted?.Profession != null)
                        jobProfession = extracted.Profession;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Job ad chunk parsing failed for response: {Response}. Error: {Error}", 
                        aiResult.Response?.Substring(0, Math.Min(100, (aiResult.Response?.Length ?? 0))), ex.Message);
                }
            }

            if (requirements.Count == 0)
            {
                _logger.LogWarning("No requirements extracted from job ad");
                return (new List<Requirement>(), jobProfession);
            }

            // Deduplicate requirements before embedding
            requirements = requirements
                .GroupBy(r => r.Name.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            // Filter only comparable categories
            var comparableCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "skill", "experience", "language", "degree", "certification"
            };
            
            requirements = requirements
                .Where(r => comparableCategories.Contains(r.Category?.Trim() ?? ""))
                .ToList();

            // Log Job extraction results in table format
            _logger.LogInformation("\n" +
                "════════════════════════════════════════════════════════════════\n" +
                "                       JOB AD EXTRACTION RESULTS                \n" +
                "════════════════════════════════════════════════════════════════\n" +
                "PROFESSION:\n" +
                "┌─────────────┬────────────────────────────────────────────────┐\n" +
                "│ Field       │ Value                                          │\n" +
                "├─────────────┼────────────────────────────────────────────────┤\n" +
                "│ Main        │ {Main,-46} │\n" +
                "│ Sub         │ {Sub,-46} │\n" +
                "│ Specialty   │ {Specialty,-46} │\n" +
                "└─────────────┴────────────────────────────────────────────────┘\n" +
                "\n" +
                "REQUIREMENTS ({RequirementCount} found after deduplication & filtering):\n" +
                "┌─────────────────────────────────────┬──────────────┬────────────┐\n" +
                "│ Name                                │ Category     │ Importance │\n" +
                "├─────────────────────────────────────┼──────────────┼────────────┤",
                jobProfession?.Main ?? "(empty)",
                jobProfession?.Sub ?? "(empty)",
                jobProfession?.Specialty ?? "(empty)",
                requirements.Count);

            // Log each requirement in table format
            if (requirements.Count > 0)
            {
                foreach (var req in requirements)
                {
                    _logger.LogInformation("│ {Name,-35} │ {Category,-12} │ {Importance,10} │",
                        (req.Name.Length > 35 ? req.Name.Substring(0, 32) + "..." : req.Name),
                        (req.Category.Length > 12 ? req.Category.Substring(0, 9) + "..." : req.Category),
                        req.Importance);
                }
                _logger.LogInformation("└─────────────────────────────────────┴──────────────┴────────────┘");
            }
            else
            {
                _logger.LogInformation("│ No requirements extracted                                          │\n" +
                                     "└─────────────────────────────────────┴──────────────┴────────────┘");
            }
            
            _logger.LogInformation("════════════════════════════════════════════════════════════════\n");

            return (requirements, jobProfession);
        }


    // New method to fill JobAIResponse table base on job ad in jobs table and AIMessageTemplates[1]


        // New method to read job ad from jobs table and return String URL
        public async Task<List<(int JobId, string WebpageUrl)>> GetAllJobURLsFromDatabaseAsync()
        {
            _logger.LogInformation("Retrieving all job URLs from database...");

            List<(int JobId, string WebpageUrl)> result = new();

            try
            {
                var jobs = await _context.Jobs
                    .Select(j => new { j.JobId, j.WebpageUrl })
                    .ToListAsync();

                foreach (var j in jobs)
                {
                    try
                    {
                        if (
                            !string.IsNullOrEmpty(j.WebpageUrl) &&
                            !j.WebpageUrl.Trim().Equals("Unknown URL", StringComparison.OrdinalIgnoreCase) &&
                            !j.WebpageUrl.Trim().Equals("n/a", StringComparison.OrdinalIgnoreCase) &&
                            j.WebpageUrl.StartsWith("http")
                        )
                        {
                            result.Add((j.JobId, j.WebpageUrl));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception when processing job. JobId: {JobId}, WebpageUrl: {WebpageUrl}", j.JobId, j.WebpageUrl);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when reading jobs from DB.");
                throw;
            }

            _logger.LogInformation($"Retrieved {result.Count} jobs with valid URLs from database");
            return result;
        }

        // New method to fill JobAIResponse table base on job ad in jobs table and AIMessageTemplates[1]
        // Helper method with retry logic for API calls
        private async Task<string?> ExecuteAIAnalysisWithRetryAsync(string webpageUrl, int jobId, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await GetOrCreateAnalysisAsync(webpageUrl, jobId);
                    return result;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("503") || ex.Message.Contains("502") || ex.Message.Contains("429"))
                {
                    if (attempt == maxRetries)
                    {
                        _logger.LogError($"Failed to process JobId: {jobId} after {maxRetries} attempts. Last error: {ex.Message}");
                        throw;
                    }
                    
                    int backoffDelay = attempt * 2000; // Exponential backoff: 2s, 4s, 6s
                    _logger.LogWarning($"Server busy for JobId: {jobId}, attempt {attempt}/{maxRetries}. Retrying in {backoffDelay}ms...");
                    await Task.Delay(backoffDelay);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unexpected error processing JobId: {jobId}, attempt {attempt}: {ex.Message}");
                    if (attempt == maxRetries) throw;
                    await Task.Delay(1000);
                }
            }
            
            return null;
        }

        // Optimerad metod med Polly, Checkpointing och Concurrency Control
        public async Task ProcessAllJobsWithAIAsync(int batchSize = 10, int delayBetweenBatches = 3000, CancellationToken cancellationToken = default)
        {
            var allJobs = await GetAllJobURLsFromDatabaseAsync();

            for (int i = 0; i < allJobs.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = allJobs.Skip(i).Take(batchSize).ToList();
                _logger.LogInformation($"Processing batch {i / batchSize + 1}/{Math.Ceiling((double)allJobs.Count / batchSize)} ({batch.Count} jobs)");

                // SEKVENTIELL - EN JOBB I TAGET!
                foreach (var (jobId, url) in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await ProcessSingleJobWithCheckpointAsync(jobId, url); // VÄNTA på varje!
                        await Task.Delay(500, cancellationToken); // 0.5s paus mellan jobs (API rate limit)
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed job {jobId}");
                    }
                }

                // Paus mellan batcher
                if (i + batchSize < allJobs.Count)
                {
                    _logger.LogInformation($"Batch completed. Waiting {delayBetweenBatches}ms...");
                    await Task.Delay(delayBetweenBatches, cancellationToken);
                }
            }
        }

        // Enum för resultat-typer
        private enum JobProcessResult
        {
            Processed,
            Skipped,
            Error
        }

        // Metod som sparar varje jobb direkt efter bearbetning (Checkpointing)
        private async Task<JobProcessResult> ProcessSingleJobWithCheckpointAsync(int jobId, string webpageUrl)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Check if already successfully processed
                var existingResponse = await _context.JobAIResponses
                    .Where(r => r.JobId == jobId && r.Status == JobStatus.Completed)
                    .FirstOrDefaultAsync();
                
                if (existingResponse != null)
                {
                    _logger.LogDebug($"Skipped JobId: {jobId} - already successfully processed");
                    return JobProcessResult.Skipped;
                }
                
                _logger.LogInformation($"Processing JobId: {jobId}, URL: {webpageUrl}");
                
                // CREATE PROCESSING RECORD
                var processingResponse = new JobAIResponse
                {
                    JobId = jobId,
                    ResponseText = "{}", 
                    Status = JobStatus.Processing
                    // CreatedAt sätts automatiskt till DateTime.UtcNow
                };
                _context.JobAIResponses.Add(processingResponse);
                await _context.SaveChangesAsync();
                
                // Execute AI analysis with Polly retry logic
                var result = await ExecuteAIAnalysisWithPollyAsync(webpageUrl, jobId);
                
                stopwatch.Stop();
                
                if (!string.IsNullOrEmpty(result))
                {
                    // UPDATE TO COMPLETED
                    processingResponse.ResponseText = result;
                    processingResponse.Status = JobStatus.Completed;
                    processingResponse.CreatedAt = DateTime.UtcNow; // Update completion time
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"✅ JobId: {jobId} Status: {JobStatus.Completed}, Duration: {stopwatch.ElapsedMilliseconds}ms");
                    return JobProcessResult.Processed;
                }
                else
                {
                    // UPDATE TO FAILED
                    processingResponse.Status = JobStatus.Failed;
                    processingResponse.CreatedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogWarning($"❌ JobId: {jobId} Status: {JobStatus.Failed} - DeepSeek returned no result");
                    return JobProcessResult.Error;
                }
            }
            catch (Exception ex)
            {
                // UPDATE TO FAILED WITH ERROR
                try
                {
                    var failedResponse = await _context.JobAIResponses
                        .Where(r => r.JobId == jobId && r.Status == JobStatus.Processing)
                        .FirstOrDefaultAsync();
                    
                    if (failedResponse != null)
                    {
                        failedResponse.Status = JobStatus.Failed;
                        failedResponse.CreatedAt = DateTime.UtcNow;
                        
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Updated JobId: {jobId} Status: {JobStatus.Failed}");
                    }
                }
                catch (Exception saveEx)
                {
                    _logger.LogError($"Failed to update error status for JobId: {jobId} - {saveEx.Message}");
                }
                
                _logger.LogError($"❌ Error processing JobId: {jobId}, URL: {webpageUrl} - {ex.Message}");
                return JobProcessResult.Error;
            }
        }

        // Ny metod som använder Polly för robust retry-hantering
        private async Task<string?> ExecuteAIAnalysisWithPollyAsync(string webpageUrl, int jobId)
        {
            var context = new Context($"JobId-{jobId}")
            {
                ["Logger"] = _logger
            };
            
            try
            {
                using var httpClient = new HttpClient();
                
                var response = await DeepSeekRetryPolicy.ExecuteAsync(async (ctx) =>
                {
                    // Build AI request med optimerad context caching
                    var user = await _userPropertiesService.GetCurrentUserAsync();
                    var request = new AIRequest
                    {
                        CommandId = 1, // AIMessageTemplates[1] för jobb-analys
                        JobAdUrl = webpageUrl,
                        User = user
                    };
                    
                    // ADD: Log before AI request building
                    _logger.LogInformation($"🔧 Building AI request for JobId: {jobId}");
                    
                    var requestContent = await _aiRequestBuilderService.AIRequestBuilder(request);
                    
                    var apiUrl = AIConfig.OpenAIUrl;
                    if (string.IsNullOrEmpty(AIConfig.OpenAIKey))
                        throw new InvalidOperationException("OpenAI API key is not configured.");

                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AIConfig.OpenAIKey}");
                    
                    // ADD: Log before API call
                    _logger.LogInformation($"🚀 Sending request to DeepSeek API: {apiUrl} for JobId: {jobId}");
                    
                    var httpResponse = await httpClient.PostAsync(apiUrl, requestContent);
                    
                    // ADD: Log API response status
                    _logger.LogInformation($"📡 DeepSeek API responded with {httpResponse.StatusCode} for JobId: {jobId}");
                    
                    return httpResponse;
                    
                }, context);

                if (response == null)
                    throw new Exception("HTTP response is null.");

                var responseContent = await response.Content.ReadAsStringAsync();
                if (responseContent == null)
                    throw new Exception("AI response content is null.");

                // ADD: Log response processing
                _logger.LogInformation($"📄 AI response received, length: {responseContent.Length} characters for JobId: {jobId}");

                var cleanJson = _manageIOService.ExtractInnerJsonFromJobAIResponse(responseContent);
                
                // ADD: Log JSON extraction result
                if (string.IsNullOrEmpty(cleanJson))
                {
                    _logger.LogWarning($"⚠️ JSON extraction failed for JobId: {jobId}");
                }
                else
                {
                    _logger.LogInformation($"✅ JSON extracted successfully, length: {cleanJson.Length} characters for JobId: {jobId}");
                }
                
                return cleanJson;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to process JobId: {jobId} with Polly after all retries. Error: {ex.Message}");
                return null;
            }
        }

        // ...existing code för GetAllJobURLsFromDatabaseAsync och andra metoder...
    }
}

// Extension för Context
public static class ContextExtensions
{
    public static ILogger? GetLogger(this Context context)
    {
        return context.TryGetValue("Logger", out var logger) ? logger as ILogger : null;
    }
}