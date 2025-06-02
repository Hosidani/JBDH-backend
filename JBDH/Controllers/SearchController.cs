using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JBDH.Models;
using JBDH.Services;
using NLog.Config;
using Markdig;

namespace JBDH.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserPropertiesService _userPropertiesService;
        private readonly IAIHelperSwitchService _aiHelperSwitchService;
        private readonly IJobAIAnalysisService _jobAIAnalysisService;
        private readonly ISearchCandidateService _searchCandidateService; // Added service for candidate search

        public SearchController(ApplicationDbContext context, IWebHostEnvironment environment, IUserPropertiesService userPropertiesService, IAIHelperSwitchService aiHelperSwitchService, IJobAIAnalysisService jobAIAnalysisService, ISearchCandidateService searchCandidateService)
        {
            _context = context;
            _environment = environment;
            _userPropertiesService = userPropertiesService;
            _aiHelperSwitchService = aiHelperSwitchService;
            _jobAIAnalysisService = jobAIAnalysisService;
            _searchCandidateService = searchCandidateService; // Initialize the candidate search service
        }

        [HttpGet]
        public async Task<IActionResult> SearchForJobs(string description, string location)
        {
            if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(location))
            {
                LoggingConfigService.ReturnError("Description or location is empty.");
                return View("Error", new ErrorViewModel
                {
                    Title = "Missing Input",
                    Message = "Description and location cannot be empty.",
                    ReturnUrl = Url.Action("Index", "Home")
                });
            }

            // Get prioritized jobs as view models
            var results = new SearchJobsService(_context, _environment)
                .SearchJobs(description, location);

            // Optionally, fetch and attach the latest AI response for each job
            foreach (var item in results)
            {
                item.LatestAIResponse = await _context.JobAIResponses
                    .Where(r => r.JobId == item.Job.JobId.ToString())
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();
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

            var existingResponse = await _context.JobAIResponses
                .Where(r => r.JobId == jobId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingResponse != null && !string.IsNullOrEmpty(existingResponse.ResponseText))
            {
                LoggingConfigService.ReturnInfo("Returning cached AI analysis result.");
                return PartialView("_JobAIResultPartial", existingResponse.ResponseText);
            }

            var aiResult = await _jobAIAnalysisService.AnalyzeJobAsync(jobAdUrl);
            string aiResultString;

            if (aiResult is System.Net.Http.StringContent stringContent)
            {
                aiResultString = await stringContent.ReadAsStringAsync();
            }
            else
            {
                aiResultString = aiResult?.ToString() ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(aiResultString) && aiResultString.TrimStart().StartsWith("{"))
            {
                try
                {
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(aiResultString);
                    aiResultString = json?.choices?[0]?.message?.content?.ToString() ?? aiResultString;
                }
                catch(Exception ex)
                {
                    LoggingConfigService.ReturnError("Failed to parse AI response JSON: " + ex.Message);
                    // Ignore parsing errors and use the original string
                }
            }

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

            var newResponse = new JobAIResponse
            {
                JobId = jobId,
                ResponseText = aiResultString,
                CreatedAt = DateTime.UtcNow
            };
            _context.JobAIResponses.Add(newResponse);
            await _context.SaveChangesAsync();

            LoggingConfigService.ReturnInfo($"AI analysis result: {aiResultString}");
            return PartialView("_JobAIResultPartial", aiResultString);
        }

        [HttpGet]
        public IActionResult SearchCandidate(string keyword, string location)
        {
            if (string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(location))
            {
                return View();
            }
            var results = _searchCandidateService.SearchCandidates(keyword, location);
            ViewData["keyword"] = keyword; 
            return View("SearchResultsCandidates", results);
        }
    }
}