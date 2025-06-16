using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JBDH.Models;
using JBDH.Services;
using JBDH.Services.Interfaces;
using JBDH.Services.AICommunications.Interfaces;
using NLog.Config;
using System.Threading.Tasks;
using System.Security.Claims;

namespace JBDH.Controllers
{
    public class SearchController : Controller
    {
        private readonly IUserPropertiesService _userPropertiesService;
        private readonly IAIRequestBuilderService _aIRequestBuilderService;
        private readonly ISearchCandidateService _searchCandidateService; // Added service for candidate search
        private readonly IAIApiClient _aIService;
        private readonly ISearchJobsService _searchJobsService;
        private readonly IUserAIResponseService _userAIResponseService;
        private readonly IJobAIResponseService _jobAIResponseService;
        private readonly IJobAIAnalysisService _jobAIAnalysisService;
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
            IUserAIResponseService userAIResponseService)
        {
            _userPropertiesService = userPropertiesService;
            _aIRequestBuilderService = aIRequestBuilderService;
            _searchCandidateService = searchCandidateService;
            _aIService = aIService;
            _searchJobsService = searchJobsService;
            _jobAIResponseService = jobAIResponseService;
            _jobAIAnalysisService = jobAIAnalysisService;
            _userAIResponseService = userAIResponseService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SearchForJobs(string description, string location)
        {
            // Only show error if both are empty
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

            // Get prioritized jobs as view models
            var results = await _searchJobsService.SearchJobsAsync(description, location);

            // Attach latest AI responses using a service
            await _jobAIResponseService.AttachLatestAIResponsesAsync(results);

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId = 0;
            if (!string.IsNullOrEmpty(userIdStr))
                int.TryParse(userIdStr, out userId);

            foreach (var item in results)
            {
                item.HasApplied = userId > 0 &&
                    await _context.JobApplications.AnyAsync(a => a.JobId.ToString() == item.Job.JobId && a.UserId == userId);
            }

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

            LoggingConfigService.ReturnInfo($"AI analysis result: {aiResultString}");
            return PartialView("_JobAIResultPartial", aiResultString);
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
            if (user == null || string.IsNullOrEmpty(description) || string.IsNullOrEmpty(location) || string.IsNullOrEmpty(user.CVPath))
            {
                LoggingConfigService.ReturnError("Description, location, or CV path is empty.");
                return View("Error", new ErrorViewModel
                {
                    Title = "Missing Input",
                    Message = "Description, location, and CV path cannot be empty.",
                    ReturnUrl = Url.Action("Index", "Home")
                });
            }

            // Get prioritized jobs as view models
            var results = await _searchJobsService.SearchJobsAsync(description, location);

            // Analyze the first job against the CV using AIHelperSwitchService
            if (results.Any() && results.First().Job != null)
            {
                var aiRequest = new AIRequest
                {
                    CommandId = 2,
                    JobAdUrl = results.First().Job.WebpageUrl,
                    CVPath = user.CVPath ?? "",
                    User = user
                };
                string aiAnalysisResult = await _aIService.GetAIResponse(aiRequest);
                ViewData["AIAnalysisResult"] = aiAnalysisResult;
            }
            else
            {
                ViewData["AIAnalysisResult"] = "No jobs found for the given criteria.";
            }

            return View("SearchResultsJobs", results);
        }
    }
}