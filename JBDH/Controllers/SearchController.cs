using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using JBDH.Models.Core;
using JBDH.Models.AI;
using JBDH.Models.ViewModels;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.JobManagement;
using JBDH.Services.Configuration;
using JBDH.Services.Workflows;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog.Config;
using Newtonsoft.Json;

namespace JBDH.Controllers
{
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

        public SearchController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IUserPropertiesService userPropertiesService,
            IAIRequestBuilderService aIRequestBuilderService,
            ISearchCandidateService searchCandidateService,
            IAIApiClient aIService,
            ISearchJobsService searchJobsService,
            IJobAIResponseService jobAIResponseService,
            IJobAIAnalysisService jobAIAnalysisService,
            IUserAIResponseService userAIResponseService,
            ISearchWorkflowService searchWorkflowService)
        {
            _userPropertiesService = userPropertiesService;
            _aIRequestBuilderService = aIRequestBuilderService;
            _searchCandidateService = searchCandidateService;
            _aIService = aIService;
            _searchJobsService = searchJobsService;
            _jobAIResponseService = jobAIResponseService;
            _jobAIAnalysisService = jobAIAnalysisService;
            _userAIResponseService = userAIResponseService;
            _searchWorkflowService = searchWorkflowService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SearchForJobs(string description, string location)
        {
            if (string.IsNullOrEmpty(description) && string.IsNullOrEmpty(location))
            {
                LoggingConfigService.ReturnError("Both description and location are empty.");
                return View("Error", new ErrorViewModel
                {
                    Title = "Missing Input",
                    Message = "Please provide at least a description or a location.",
                    ReturnUrl = Url.Action("Index", "Home")
                });
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                userId = parsedId;

            var results = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            return View("SearchResultsJobs", results);
        }

        public IActionResult SearchJob()
        {
            return View();
        }

        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeJob(string jobAdUrl, string jobId)
        {
            LoggingConfigService.ReturnInfo($"AnalyzeJob called with URL: {jobAdUrl} and Job ID: {jobId}");

            var aiResultString = await _jobAIAnalysisService.GetOrCreateAnalysisAsync(jobAdUrl, jobId);

            if (string.IsNullOrEmpty(aiResultString))
            {
                LoggingConfigService.ReturnError("AI analysis returned an empty result.");
                return View("Error", new ErrorViewModel
                {
                    Title = "AI Analysis Error",
                    Message = "The AI analysis did not return any results.",
                    ReturnUrl = Url.Action("SearchJob")
                });
            }

            // --- Extract JSON from code block if present ---
            if (aiResultString.Contains("```"))
            {
                var start = aiResultString.IndexOf("{");
                var end = aiResultString.LastIndexOf("}");
                if (start >= 0 && end > start)
                    aiResultString = aiResultString.Substring(start, end - start + 1);
            }

            JobAdAIResponseViewModel? jobAdModel = null;
            try
            {
                jobAdModel = JsonConvert.DeserializeObject<JobAdAIResponseViewModel>(aiResultString);
            }
            catch
            {
                LoggingConfigService.ReturnError("Failed to parse job ad AI analysis result to ViewModel.");
            }

            return PartialView("_JobAdAIResultPartial", jobAdModel);
        }

        [HttpGet]
        public async Task<IActionResult> SearchCandidate(string keyword, string location)
        {
            if (string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(location))
            {
                return View();
            }
            var results = await _searchCandidateService.SearchCandidates(keyword, location);
            ViewData["keyword"] = keyword;
            return View("SearchResultsCandidates", results);
        }

        [HttpGet]
        public async Task<IActionResult> SearchForJobsAndAnalyzeCVAgainstJob(string description, string location)
        {
            LoggingConfigService.ReturnInfo("SearchForJobsAndAnalyzeCVAgainstJob called.");
            var user = await _userPropertiesService.GetCurrentUserAsync();

            if ((string.IsNullOrEmpty(description) && string.IsNullOrEmpty(location)) || user == null || string.IsNullOrEmpty(user.CVPath))
            {
                LoggingConfigService.ReturnError("Please provide at least a description or a location, and make sure your CV is uploaded.");
                return View("Error", new ErrorViewModel
                {
                    Title = "Missing Input",
                    Message = "Please provide at least a description or a location, and make sure your CV is uploaded.",
                    ReturnUrl = Url.Action("Index", "Home")
                });
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                userId = parsedId;

            // Use workflow service for search and meta
            var results = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            if (results.Any() && results.First().Job != null)
            {
                var aiRequest = new AIRequest
                {
                    CommandId = 2,
                    JobAdUrl = results.First().Job.WebpageUrl,
                    CVPath = user.CVPath,
                    User = user
                };
                string aiAnalysisResult = await _aIService.GetAIResponse(aiRequest);

                // --- Extract JSON from code block if present ---
                if (aiAnalysisResult.Contains("```"))
                {
                    var start = aiAnalysisResult.IndexOf("{");
                    var end = aiAnalysisResult.LastIndexOf("}");
                    if (start >= 0 && end > start)
                        aiAnalysisResult = aiAnalysisResult.Substring(start, end - start + 1);
                }

                // Parse JSON to ViewModel
                CVJobComparisonAIResponseViewModel? aiModel = null;
                try
                {
                    aiModel = JsonConvert.DeserializeObject<CVJobComparisonAIResponseViewModel>(aiAnalysisResult);
                }
                catch
                {
                    LoggingConfigService.ReturnError("Failed to parse AI analysis result to ViewModel.");
                }

                ViewData["AIAnalysisModel"] = aiModel;
            }
            else
            {
                ViewData["AIAnalysisModel"] = null;
            }

            return View("SearchResultsJobs", results);
        }
    }
}