using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Newtonsoft.Json;
using JBDH.Models.Core;
using JBDH.Models.AI;
using JBDH.Models.ViewModels;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.JobManagement;
using JBDH.Services.Configuration;
using JBDH.Services.Workflows;
using JBDH.Services.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog.Config;
using JBDH.Models.User;

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
        private readonly IManageIOService _manageIOService; 
        private readonly IMatchingAlgorithmCVJobService _matchingAlgorithmCVJobService;

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
            IMatchingAlgorithmCVJobService matchingAlgorithmCVJobService
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

            if (!int.TryParse(jobId, out int parsedJobId))
            {
                LoggingConfigService.ReturnError($"Invalid job ID format: {jobId}");
                return Content("Invalid job ID");
            }

            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return Content("User not found");

            var aiResultString = await _jobAIAnalysisService.GetOrCreateAnalysisAsync(jobAdUrl, parsedJobId);

            LoggingConfigService.ReturnInfo($"job AI response: {aiResultString}");

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
                return View("Error", new ErrorViewModel
                {
                    Title = "Parsing Error",
                    Message = "Could not parse the AI analysis result.",
                    ReturnUrl = Url.Action("SearchJob")
                });
            }

            // --- Calculate CV Match Percent ---
            int matchPercent = 0;
            var userAIResponse = await _userAIResponseService.GetLatestUserAIResponseAsync(user.Id);
            if (userAIResponse == null)
                Console.WriteLine("userAIResponse is null");
            else if (string.IsNullOrEmpty(userAIResponse.ResponseText))
                Console.WriteLine("userAIResponse.ResponseText is null or empty");
            else if (jobAdModel == null)
                Console.WriteLine("jobAdModel is null");
            else
            {
                var cvAI = JsonConvert.DeserializeObject<CVAnalysisResult>(userAIResponse.ResponseText);
                if (cvAI != null)
                    matchPercent = _matchingAlgorithmCVJobService.CalculateJobTitleMatchPercentage(jobAdModel, cvAI);
            }
            var percentHtml = $"<span class='badge bg-info'>CV Match: {matchPercent}%</span>";

            // --- Return for AJAX or normal partial ---
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Render the partial view as string
                var analysisHtml = await this.RenderViewAsync("_JobAdAIResultPartial", jobAdModel, true);
                return Json(new { percentHtml, analysisHtml });
            }
            else
            {
                // Fallback for non-AJAX
                ViewBag.CVMatchPercent = matchPercent;
                return PartialView("_JobAdAIResultPartial", jobAdModel);
            }
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
                    Message = "Please provide at least a description or a location, and make sure your CV is uploaded and you are logged in.",
                    ReturnUrl = Url.Action("Index", "Home")
                });
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                userId = parsedId;

            // Use workflow service for search and meta
            var results = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);
            LoggingConfigService.ReturnInfo($"Search results count: {results.Count}");
            /*
                        if (results.Any() && results.First().Job != null)
                        {

                            var aiRequest = new AIRequest
                            {
                                CommandId = 3,
                                JobAIResponse = await _jobAIAnalysisService.GetOrCreateAnalysisAsync(results.First().Job.WebpageUrl, results.First().Job.JobId),
                                UserAIResponse = user.AIResponses.FirstOrDefault()?.ResponseText,
                                User = user
                            };
                            LoggingConfigService.ReturnInfo($"AI Request built with CommandId: {aiRequest.CommandId}, JobAdUrl: {results.First().Job.WebpageUrl}, UserAIResponse: {user.AIResponses.FirstOrDefault()?.ResponseText}");
                            string aiAnalysisResult = await _aIService.GetAIResponse(aiRequest);
                            aiAnalysisResult = _manageIOService.ExtractJsonFromCodeBlock(aiAnalysisResult);
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
                       */
            int matchPercent = 0;
            var userAIResponse = await _userAIResponseService.GetLatestUserAIResponseAsync(user.Id);
            var jobAIResponse = await _jobAIResponseService.GetLatestJobAIResponseAsync(results.First().Job.JobId);

            var cvAI = userAIResponse != null ? JsonConvert.DeserializeObject<CVAnalysisResult>(userAIResponse.ResponseText) : null;
            var jobAI = jobAIResponse != null ? JsonConvert.DeserializeObject<JobAdAIResponseViewModel>(jobAIResponse.ResponseText) : null;


            if (cvAI != null && jobAI != null)
                matchPercent = _matchingAlgorithmCVJobService.CalculateJobTitleMatchPercentage(jobAI, cvAI);

            ViewData["JobTitleMatchPercent"] = matchPercent;
            // Return as plain text (or you can use ContentResult or Json)
            //return Content($"{matchPercent}%", "text/plain");
            // create a new ViewData dictionary to hold the data of matchningen mellan CV och job ad
            return View("SearchResultsJobs", results);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View(new List<User>());

            var users = await _userPropertiesService.SearchUsersAsync(query);
            return View(users);
        }
    }
}