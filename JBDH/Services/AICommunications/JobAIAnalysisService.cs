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
        }

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
    }
}