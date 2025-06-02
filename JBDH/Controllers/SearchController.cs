using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JBDH.Models;
using JBDH.Services;
using NLog.Config;

namespace JBDH.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IUserPropertiesService _userPropertiesService;
        private readonly IAIHelperSwitchService _aiHelperSwitchService;
        private readonly IJobAIAnalysisService _jobAIAnalysisService;

        public SearchController(ApplicationDbContext context, IWebHostEnvironment environment, IUserPropertiesService userPropertiesService, IAIHelperSwitchService aiHelperSwitchService, IJobAIAnalysisService jobAIAnalysisService)
        {
            _context = context;
            _environment = environment;
            _userPropertiesService = userPropertiesService;
            _aiHelperSwitchService = aiHelperSwitchService;
            _jobAIAnalysisService = jobAIAnalysisService;
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

            return View("SearchResults", results);
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

            // Extract only the AI's answer if the result is JSON
            if (!string.IsNullOrEmpty(aiResultString) && aiResultString.TrimStart().StartsWith("{"))
            {
                try
                {
                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(aiResultString);
                    aiResultString = json?.choices?[0]?.message?.content?.ToString() ?? aiResultString;
                }
                catch
                {
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
            LoggingConfigService.ReturnInfo($"AI analysis result: {aiResultString}");
            return PartialView("_JobAIResultPartial", aiResultString);
        }
    }
}