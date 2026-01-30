using System.Security.Claims;

using Newtonsoft.Json;
using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.JobManagement;
using JBDH.Services.Configuration;
using JBDH.Services.Workflows;
using JBDH.Services.Helpers;
using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using JBDH.Helpers;
using JBDH.Models.User;
using JBDH.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Controllers
{
    [IncludeInApi]
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : Controller
    {
        private readonly IUserPropertiesService _userPropertiesService;
        private readonly IAIRequestBuilderService _aIRequestBuilderService;
        private readonly ISearchCandidateService _searchCandidateService;
        private readonly IAIApiClient _aIService;
        private readonly ISearchJobsService _searchJobsService;
        private readonly IUserAIResponseService _userAIResponseService;
        private readonly IJobAIResponseService _jobAIResponseService;
        private readonly IJobAIAnalysisService _jobAIAnalysisService;
        private readonly ISearchWorkflowService _searchWorkflowService;
        private readonly ApplicationDbContext _context;
        private readonly IManageIOService _manageIOService; 
        private readonly IMatchingAlgorithmCVJobService _matchingAlgorithmCVJobService;
        private readonly ISavedJobService _savedJobService;

        public SearchController(
            IUserPropertiesService userPropertiesService,
            IAIRequestBuilderService aIRequestBuilderService,
            ISearchCandidateService searchCandidateService,
            IAIApiClient aIService,
            ISearchJobsService searchJobsService,
            IUserAIResponseService userAIResponseService,
            IJobAIResponseService jobAIResponseService,
            IJobAIAnalysisService jobAIAnalysisService,
            ISearchWorkflowService searchWorkflowService,
            ApplicationDbContext context,
            IManageIOService manageIOService ,
            IMatchingAlgorithmCVJobService matchingAlgorithmCVJobService,
            ISavedJobService savedJobService
        )
        {
            _userPropertiesService = userPropertiesService;
            _aIRequestBuilderService = aIRequestBuilderService;
            _searchCandidateService = searchCandidateService;
            _aIService = aIService;
            _searchJobsService = searchJobsService;
            _userAIResponseService = userAIResponseService;
            _jobAIResponseService = jobAIResponseService;
            _jobAIAnalysisService = jobAIAnalysisService;
            _searchWorkflowService = searchWorkflowService;
            _context = context;
            _manageIOService = manageIOService; 
            _matchingAlgorithmCVJobService = matchingAlgorithmCVJobService;
            _savedJobService = savedJobService;
        }

        [HttpGet("SearchForJobs")]
        public async Task<IActionResult> SearchForJobs(string? description = null, string? location = null)
        {
            // Allow empty parameters - will search all jobs if both are empty

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                userId = parsedId;

            // Use SearchJobsService to get JobSearchResultViewModel objects
            var searchResults = await _searchJobsService.SearchJobsAsync(description ?? "", location ?? "");
            
            LoggingConfigService.ReturnInfo($"Search returned {searchResults.Count} jobs for description='{description}', location='{location}'");
            
            // Filter out not interested jobs for logged in users
            if (userId.HasValue)
            {
                // Get not interested job IDs
                var notInterestedJobIds = await _context.NotInterestedJobs
                    .Where(nij => nij.UserId == userId.Value)
                    .Select(nij => nij.JobId)
                    .ToListAsync();
                
                // Filter out not interested jobs
                searchResults = searchResults
                    .Where(sr => sr.Job != null && !notInterestedJobIds.Contains(sr.Job.JobId))
                    .ToList();
                
                LoggingConfigService.ReturnInfo($"After filtering out {notInterestedJobIds.Count} not interested jobs: {searchResults.Count} jobs remain");
            }
            
            // Convert to JobDetailsDto format for React
            var jobDetails = new List<JobDetailsDto>();

            foreach (var searchResult in searchResults)
            {
                if (searchResult.Job != null)
                {
                    var isJobSaved = userId.HasValue ? await _savedJobService.IsJobSavedAsync(userId.Value, searchResult.Job.JobId) : false;
                    var jobDto = JobDetailsDto.FromJob(searchResult.Job, false, isJobSaved, false, null);
                    LoggingConfigService.ReturnInfo($"Processing job: {searchResult.Job.JobId}, Title: '{searchResult.Job.Title}', Company: '{searchResult.Job.Company}', match score: {jobDto.MatchCount} ");
                    jobDetails.Add(jobDto);
                }
                else
                {
                    LoggingConfigService.ReturnError("searchResult.Job is null!");
                }
            }
            
            LoggingConfigService.ReturnInfo($"Returning {jobDetails.Count} JobDetailsDto objects");

            return Ok(jobDetails); // Return as JobDetailsDto for React
        }

        public IActionResult SearchJob()
        {
            return Ok(new { message = "SearchJob endpoint is available." });
        }

        public IActionResult Search()
        {
            return Ok(new { message = "Search endpoint is available." });
        }
        public class AnalyzeJobRequest
        {
            public string JobAdUrl { get; set; } = "";
            public string JobId { get; set; } = "";
        }


        [HttpPost]
        public async Task<IActionResult> AnalyzeJob([FromBody] AnalyzeJobRequest request)
        {
            string jobAdUrl = request.JobAdUrl;
            string jobId = request.JobId;
            
            LoggingConfigService.ReturnInfo($"AnalyzeJob called with URL: {jobAdUrl} and Job ID: {jobId}");

            if (!int.TryParse(jobId, out int parsedJobId))
            {
                LoggingConfigService.ReturnError($"Invalid job ID format: {jobId}");
                return BadRequest(new { error = "Invalid job ID" });
            }

            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return NotFound(new { error = "User not found" });

            var aiResultString = await _jobAIAnalysisService.GetOrCreateAnalysisAsync(jobAdUrl, parsedJobId);

            LoggingConfigService.ReturnInfo($"job AI response: {aiResultString}");

            if (string.IsNullOrEmpty(aiResultString))
            {
                LoggingConfigService.ReturnError("AI analysis returned an empty result.");
                return NotFound(new { error = "The AI analysis did not return any results." });
            }

            // Extract and parse
            var cleanJson = _manageIOService.ExtractInnerJsonFromJobAIResponse(aiResultString);
            JobAdAIResponseViewModel? jobAdModel = null;
            try
            {
                jobAdModel = JsonConvert.DeserializeObject<JobAdAIResponseViewModel>(cleanJson);
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Failed to parse job ad AI analysis result: {ex.Message}");
                return BadRequest(new { error = "Could not parse the AI analysis result." });
            }

            // --- Calculate CV Match Percent ---
            int matchPercent = 0;
            var userAIResponse = await _userAIResponseService.GetLatestUserAIResponseAsync(user.Id);
            if (userAIResponse != null && !string.IsNullOrEmpty(userAIResponse.ResponseText) && jobAdModel != null)
            {
                var cvAI = JsonConvert.DeserializeObject<CVAnalysisResult>(userAIResponse.ResponseText);
                if (cvAI != null)
                    matchPercent = _matchingAlgorithmCVJobService.CalculateJobTitleMatchPercentage(jobAdModel, cvAI);
            }

            // Return all relevant data as JSON
            return Ok(new {
                jobAdModel,
                matchPercent
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchCandidate(string keyword, string location)
        {
            if (string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(location))
            {
                return BadRequest(new { error = "Please provide a keyword or location." });
            }
            var results = await _searchCandidateService.SearchCandidates(keyword, location);
            return Ok(new { keyword, results });
        }

        [HttpGet]
        public async Task<IActionResult> SearchForJobsAndAnalyzeCVAgainstJob(string? description = null, string? location = null)
        {
            LoggingConfigService.ReturnInfo("SearchForJobsAndAnalyzeCVAgainstJob called.");
            var user = await _userPropertiesService.GetCurrentUserAsync();

            if ((string.IsNullOrEmpty(description) && string.IsNullOrEmpty(location)) || user == null || string.IsNullOrEmpty(user.CVPath))
            {
                LoggingConfigService.ReturnError("Please provide at least a description or a location, and make sure your CV is uploaded.");
                return BadRequest(new { error = "Please provide at least a description or a location, and make sure your CV is uploaded and you are logged in." });
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                userId = parsedId;

            // Use workflow service for search and meta - convert null to empty string
            var results = await _searchWorkflowService.SearchAndAttachMetaAsync(description ?? "", location ?? "", userId);
            LoggingConfigService.ReturnInfo($"Search results count: {results.Count}");

            int matchPercent = 0;
            CVAnalysisResult? cvAI = null;
            JobAdAIResponseViewModel? jobAI = null;

            var userAIResponse = await _userAIResponseService.GetLatestUserAIResponseAsync(user.Id);
            if (userAIResponse != null && !string.IsNullOrEmpty(userAIResponse.ResponseText))
            {
                cvAI = JsonConvert.DeserializeObject<CVAnalysisResult>(userAIResponse.ResponseText);
            }

            if (results != null && results.Count > 0 && results.First().Job != null)
            {
                var firstJob = results.First().Job;
                if (firstJob != null)
                {
                    var jobAIResponse = await _jobAIResponseService.GetLatestJobAIResponseAsync(firstJob.JobId);
                    if (jobAIResponse != null && !string.IsNullOrEmpty(jobAIResponse.ResponseText))
                    {
                        jobAI = JsonConvert.DeserializeObject<JobAdAIResponseViewModel>(jobAIResponse.ResponseText);
                    }
                }
            }

            if (cvAI != null && jobAI != null)
                matchPercent = _matchingAlgorithmCVJobService.CalculateJobTitleMatchPercentage(jobAI, cvAI);

            // Return all relevant data as JSON
            return Ok(new {
                results,
                matchPercent
            });
        }


        [HttpGet("searchV2")]
        public async Task<IActionResult> SearchJobsV2([FromQuery] JobSearchRequestDto request)
        {
            LoggingConfigService.ReturnInfo("SearchJobs V2 called - Optimized version");
            
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                userId = parsedId;

            // STEP 1: Single query to get Jobs + latest JobAIResponse
            var jobsWithAI = await GetJobsWithLatestAIResponseAsync(request);
            LoggingConfigService.ReturnInfo($"Loaded {jobsWithAI.Count} jobs with AI data in single query");

            // STEP 2: In-memory filtering and deserialization
            var matchingJobs = await FilterJobsInMemoryAsync(jobsWithAI, request, userId);
            LoggingConfigService.ReturnInfo($"After filtering: {matchingJobs.Count} jobs match criteria");

            // Skriv ut alla JobId
            var jobIds = matchingJobs.Select(j => j.JobId).ToList();
            LoggingConfigService.ReturnInfo("JobIds: " + string.Join(", ", jobIds));


            // Sortera så att de med flest träffar kommer först
            matchingJobs = matchingJobs.OrderByDescending(j => j.MatchCount).ToList();
            /*
            foreach (var job in matchingJobs)
            {
                LoggingConfigService.ReturnInfo($"JobId: {job.JobId}, match score: {job.MatchCount}");
            }
            */
            return Ok(matchingJobs);
        }

        // OPTIMIZED: Single query with join and subquery for latest AI response
        private async Task<List<JobWithAIData>> GetJobsWithLatestAIResponseAsync(JobSearchRequestDto request)
        {
            var query = _context.Jobs.AsQueryable();

            // Basic filtering at DB level
            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                var description = request.Description;
                query = query.Where(j => (j.Title != null && j.Title.Contains(description!)) || 
                                       (j.Description != null && j.Description.Contains(description!)));
            }
            
            if (!string.IsNullOrWhiteSpace(request.Location))
            {
                query = query.Where(j => j.Location != null && j.Location.Contains(request.Location!));
            }

            // Single query with LEFT JOIN to get latest JobAIResponse per job
            var jobsWithAI = await query
                .Select(j => new JobWithAIData
                {
                    Job = j,
                    LatestAIResponse = _context.JobAIResponses
                        .Where(jar => jar.JobId == j.JobId)
                        .OrderByDescending(jar => jar.CreatedAt)
                        .Select(jar => jar.ResponseText)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return jobsWithAI;
        }

        // OPTIMIZED: In-memory processing with single deserialization per job
        private async Task<List<JobDetailsDto>> FilterJobsInMemoryAsync(
            List<JobWithAIData> jobsWithAI, 
            JobSearchRequestDto request, 
            int? userId)
        {
            var matchingJobs = new List<JobDetailsDto>();
            var hasAICriteria = HasAISearchCriteria(request);

            // Get user-specific data in batch queries if userId exists
            Dictionary<int, bool>? savedJobsLookup = null;
            Dictionary<int, bool>? appliedJobsLookup = null;
            HashSet<int>? notInterestedJobIds = null;

            if (userId.HasValue)
            {
                var jobIds = jobsWithAI.Select(j => j.Job.JobId).ToList();
                
                // Batch load all user-specific data
                savedJobsLookup = await _context.SavedJobs
                    .Where(sj => sj.UserId == userId.Value && jobIds.Contains(sj.JobId))
                    .ToDictionaryAsync(sj => sj.JobId, sj => true);

                appliedJobsLookup = await _context.JobApplications
                    .Where(ja => ja.UserId == userId.Value && jobIds.Contains(ja.JobId))
                    .ToDictionaryAsync(ja => ja.JobId, ja => true);

                notInterestedJobIds = await _context.NotInterestedJobs
                    .Where(nij => nij.UserId == userId.Value && jobIds.Contains(nij.JobId))
                    .Select(nij => nij.JobId)
                    .ToHashSetAsync();
            }

            // Process jobs in memory
            foreach (var jobWithAI in jobsWithAI)
            {
                var job = jobWithAI.Job;

                // Skip not interested jobs
                if (notInterestedJobIds?.Contains(job.JobId) == true)
                    continue;

                // Location måste matcha om det är angivet
                if (!string.IsNullOrWhiteSpace(request.Location) &&
                    (job.Location == null || !job.Location.Contains(request.Location, StringComparison.OrdinalIgnoreCase)))
                {
                    continue; // Hoppa över jobb där location inte matchar
                }

                // AI-based filtering if criteria provided
                if (hasAICriteria)
                {
                    if (!await MatchesAICriteriaOptimizedAsync(request, jobWithAI))
                        continue;
                }

                // Create DTO with user-specific data
                var isJobSaved = savedJobsLookup?.GetValueOrDefault(job.JobId) == true;
                var hasApplied = appliedJobsLookup?.GetValueOrDefault(job.JobId) == true;
                
                var jobDto = JobDetailsDto.FromJob(job, hasApplied, isJobSaved);

                int matchCount = 0;
                // 1. Jobs-tabellen (JobDetailsDto)
                if (!string.IsNullOrWhiteSpace(request.Location) && jobDto.Location?.Contains(request.Location, StringComparison.OrdinalIgnoreCase) == true)
                    matchCount++;
                if (!string.IsNullOrWhiteSpace(request.Description) && jobDto.Description?.Contains(request.Description, StringComparison.OrdinalIgnoreCase) == true)
                    matchCount++;

                // 2. AI-data (JobAIAnalysisDto)
                JobAIAnalysisDto? aiData = null;
                if (!string.IsNullOrEmpty(jobWithAI.LatestAIResponse))
                {
                    try
                    {
                        aiData = JsonConvert.DeserializeObject<JobAIAnalysisDto>(jobWithAI.LatestAIResponse);
                    }
                    catch { }
                }

                if (aiData != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.Competence) &&
                        (aiData.HardRequirements.Any(req => req.Contains(request.Competence, StringComparison.OrdinalIgnoreCase)) ||
                         aiData.GoodToHaves.Any(req => req.Contains(request.Competence, StringComparison.OrdinalIgnoreCase))))
                        matchCount++;

                    if (!string.IsNullOrWhiteSpace(request.WorkArea) &&
                        (aiData.WorkArea.Any(area => area.Contains(request.WorkArea, StringComparison.OrdinalIgnoreCase)) ||
                         aiData.Experiences.Any(exp => exp.Contains(request.WorkArea, StringComparison.OrdinalIgnoreCase))))
                        matchCount++;

                    if (!string.IsNullOrWhiteSpace(request.Roles) &&
                        (aiData.JobTitle.Any(title => title.Contains(request.Roles, StringComparison.OrdinalIgnoreCase)) ||
                         aiData.Roles.Any(role => role.Contains(request.Roles, StringComparison.OrdinalIgnoreCase))))
                        matchCount++;
                }

                jobDto.MatchCount = matchCount;
                matchingJobs.Add(jobDto);
            }

            return matchingJobs;
        }

        // OPTIMIZED: Single deserialization with efficient Any() matching
        private async Task<bool> MatchesAICriteriaOptimizedAsync(
            JobSearchRequestDto request, 
            JobWithAIData jobWithAI)
        {
            // Try AI matching first
            if (!string.IsNullOrEmpty(jobWithAI.LatestAIResponse))
            {
                try
                {
                    var aiData = JsonConvert.DeserializeObject<JobAIAnalysisDto>(jobWithAI.LatestAIResponse);
                    
                    if (aiData != null)
                    {
                        return MatchesAICriteria(request, aiData);
                    }
                }
                catch (JsonException ex)
                {
                    LoggingConfigService.ReturnError($"JSON deserialization failed for job {jobWithAI.Job.JobId}: {ex.Message}");
                }
            }

            // Fallback to basic text matching
            return MatchesBasicCriteria(request, jobWithAI.Job);
        }

        // OPTIMIZED: Efficient Any() matching with OrdinalIgnoreCase
        private static bool MatchesAICriteria(JobSearchRequestDto request, JobAIAnalysisDto aiData)
        {
            // Check Competence against HardRequirements + GoodToHaves
            if (!string.IsNullOrWhiteSpace(request.Competence))
            {
                bool hasCompetence = aiData.HardRequirements.Any(req => 
                    req.Contains(request.Competence, StringComparison.OrdinalIgnoreCase)) ||
                    aiData.GoodToHaves.Any(req => 
                    req.Contains(request.Competence, StringComparison.OrdinalIgnoreCase));
                
                if (!hasCompetence) return false;
            }
            
            // Check WorkArea against WorkArea + Experiences
            if (!string.IsNullOrWhiteSpace(request.WorkArea))
            {
                bool hasWorkArea = aiData.WorkArea.Any(area => 
                    area.Contains(request.WorkArea, StringComparison.OrdinalIgnoreCase)) ||
                    aiData.Experiences.Any(exp => 
                    exp.Contains(request.WorkArea, StringComparison.OrdinalIgnoreCase));
                
                if (!hasWorkArea) return false;
            }
            
            // Check Roles against JobTitle + Roles
            if (!string.IsNullOrWhiteSpace(request.Roles))
            {
                bool hasRole = aiData.JobTitle.Any(title => 
                    title.Contains(request.Roles, StringComparison.OrdinalIgnoreCase)) ||
                    aiData.Roles.Any(role => 
                    role.Contains(request.Roles, StringComparison.OrdinalIgnoreCase));
                
                if (!hasRole) return false;
            }
            
            return true;
        }

        // Fallback for jobs without AI data
        private static bool MatchesBasicCriteria(JobSearchRequestDto request, JBDH.Models.Job.Job job)
        {
            var jobContent = $"{job.Title} {job.Description}".ToLower();
            
            if (!string.IsNullOrWhiteSpace(request.Competence) && 
                !jobContent.Contains(request.Competence, StringComparison.OrdinalIgnoreCase))
                return false;
                
            if (!string.IsNullOrWhiteSpace(request.WorkArea) && 
                !jobContent.Contains(request.WorkArea, StringComparison.OrdinalIgnoreCase))
                return false;
                
            if (!string.IsNullOrWhiteSpace(request.Roles) && 
                !jobContent.Contains(request.Roles, StringComparison.OrdinalIgnoreCase))
                return false;
            
            return true;
        }

        // Helper methods
        private static bool HasAISearchCriteria(JobSearchRequestDto request)
        {
            return !string.IsNullOrWhiteSpace(request.Competence) ||
                   !string.IsNullOrWhiteSpace(request.WorkArea) ||
                   !string.IsNullOrWhiteSpace(request.Roles);
        }

        // DTO for single query result
        private class JobWithAIData
        {
            public JBDH.Models.Job.Job Job { get; set; } = null!;
            public string? LatestAIResponse { get; set; }
        }
    }
}