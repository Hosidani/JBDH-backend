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
                    .Where(sr => !notInterestedJobIds.Contains(sr.Job.JobId))
                    .ToList();
                
                LoggingConfigService.ReturnInfo($"After filtering out {notInterestedJobIds.Count} not interested jobs: {searchResults.Count} jobs remain");
            }
            
            // Convert to JobDetailsDto format for React
            var jobDetails = new List<JobDetailsDto>();
            foreach (var searchResult in searchResults)
            {
                if (searchResult.Job != null)
                {
                    LoggingConfigService.ReturnInfo($"Processing job: {searchResult.Job.JobId}, Title: '{searchResult.Job.Title}', Company: '{searchResult.Job.Company}'");
                    
                    var isJobSaved = userId.HasValue ? await _savedJobService.IsJobSavedAsync(userId.Value, searchResult.Job.JobId) : false;
                    var jobDto = JobDetailsDto.FromJob(searchResult.Job, false, isJobSaved, false, null);
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

        [HttpPost]
        public async Task<IActionResult> AnalyzeJob(string jobAdUrl, string jobId)
        {
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
                var jobAIResponse = await _jobAIResponseService.GetLatestJobAIResponseAsync(results.First().Job.JobId);
                if (jobAIResponse != null && !string.IsNullOrEmpty(jobAIResponse.ResponseText))
                {
                    jobAI = JsonConvert.DeserializeObject<JobAdAIResponseViewModel>(jobAIResponse.ResponseText);
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

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new List<User>());

            var users = await _userPropertiesService.SearchUsersAsync(query);
            return Ok(users);
        }
    }
}