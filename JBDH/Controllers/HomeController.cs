using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JBDH.Models.ViewModels;
using JBDH.Services.DataFlow;
using JBDH.Services.Reporting;
using JBDH.Models.Core;
using JBDH.Helpers;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;
using JBDH.Models.AI;
using JBDH.Services.Helpers;
using Newtonsoft.Json.Linq;

namespace JBDH.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly DataFlowService _dataFlowService;
        private readonly IRapportUserService _rapportUserService;
        private readonly IAIRequestBuilderService _aiRequestBuilderService;
        private readonly AIConfig _aiConfig;
        private readonly IManageIOService _manageIOService;
        private readonly IJobAIAnalysisService _jobAIAnalysisService;
        private readonly IAIApiClient _aiApiClient;

        public HomeController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            DataFlowService dataFlowService,
            IRapportUserService rapportUserService,
            IAIRequestBuilderService aiRequestBuilderService,
            IManageIOService manageIOService,
            AIConfig aiConfig,
            IJobAIAnalysisService jobAIAnalysisService,
            IAIApiClient aiApiClient)
        {
            _context = context;
            _environment = environment;
            _dataFlowService = dataFlowService;
            _rapportUserService = rapportUserService;
            _aiRequestBuilderService = aiRequestBuilderService;
            _manageIOService = manageIOService;
            _aiConfig = aiConfig;
            _jobAIAnalysisService = jobAIAnalysisService;
            _aiApiClient = aiApiClient;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            if (this.IsAuthenticated())
            {
                var authResult = this.EnsureAuthenticatedUserOrRedirect();
                if (authResult.ErrorResult != null)
                    return authResult.ErrorResult;

                var userId = authResult.Value;
                var userExists = await this.UserExistsAsync(_context, userId.Value);
                if (!userExists)
                {
                    return await this.SignOutAndRedirectToHomeAsync();
                }
            }

            var dataFlows = _dataFlowService.GetAllRecursive();
            var listDataFlowViewModel = new ListDataFlowViewModel { DataFlows = dataFlows };
            return View(listDataFlowViewModel);
        }

        [Authorize]
        [HttpGet]
        public IActionResult DeleteDataFlow(int id)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect("User is not authenticated.");
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            this.LogAction($"Deleting data flow for user ID: {authResult.Value}");
            _dataFlowService.DeleteDataFlow(id);
            return this.RedirectToHome();
        }

        public IActionResult CreateFlow()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateFlow(DataFlowViewModel model)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect("User is not authenticated.");
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            var userId = authResult.Value;
            this.LogAction($"Creating new data flow with title: {model.Content}");

            if (!ModelState.IsValid)
            {
                this.SetModelInvalidError("Model state is invalid.");
                return View(model);
            }
            if (userId.HasValue && userId.Value > 0)
            {
                if (string.IsNullOrEmpty(model.Content))
                {
                    this.SetEmptyFieldError("Content");
                    return View(model);
                }
                await _dataFlowService.CreateAsync(model, userId.Value);
            }
            return this.RedirectToHome();
        }

        public IActionResult UserPosts(int userId)
        {
            var posts = _dataFlowService.GetByUserId(userId);
            return View(posts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ErrorPage()
        {
            return this.ErrorPage("An unexpected error occurred. Please try again later.");
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return View("~/Views/Settings/Settings.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ReportDataFlow(int reportedUserId, int dataFlowId, string dataFlowText, string reason)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect("User is not authenticated.");
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            var reporterUserId = authResult.Value.Value;
            var reporterExists = await this.UserExistsAsync(_context, reporterUserId);
            var reportedExists = await this.UserExistsAsync(_context, reportedUserId);
            if (!reporterExists || !reportedExists)
            {
                this.LogAction($"Reporter or reported user does not exist. Reporter ID: {reporterUserId}, Reported User ID: {reportedUserId}");
                return this.SetSuccessAndRedirect("The post has been reported.", "Index");
            }

            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, dataFlowId, dataFlowText);
            if (!result.Success)
            {
                return this.SetErrorAndRedirect(result.ErrorMessage ?? "An error occurred while reporting the post.", "Index");
            }

            return this.SetSuccessAndRedirect("The post has been reported.", "Index");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CommentOnDataFlow(int dataFlowId, string comment)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            var userId = authResult.Value;
            if (string.IsNullOrWhiteSpace(comment))
            {
                this.SetEmptyFieldError("Comment");
                return this.RedirectToHome();
            }

            await _dataFlowService.AddCommentAsync(dataFlowId, userId.Value, comment);
            return this.RedirectToHome();
        }

        [HttpPost]
        public async Task<IActionResult> CompareCvToJobAd(CompareCvToJobAdViewModel model)
        {
            LoggingConfigService.ReturnInfo("Starting CV to Job Ad comparison process.");

            var savedCvPath = await FileHelper.SaveUploadedFileAsync(model.CvPath, _environment.WebRootPath);

            var aiRequest = new AIRequest
            {
                CommandId = 5,
                CVPath = savedCvPath,
                JobAdText = model.JobAdText,
                User = null
            };

            var aiResult = await _aiApiClient.GetAIResponse(aiRequest);

            // Store the response string in TempData
            TempData["AIResult"] = aiResult.Response;
            LoggingConfigService.ReturnInfo(TempData["AIResult"] as string);

            // Redirect to GET Result
            return RedirectToAction("Result");
        }

        [HttpGet]
        public IActionResult Result()
        {
            LoggingConfigService.ReturnInfo("Accessing Result action in HomeController.");
            var aiResponse = TempData["AIResult"] as string ?? "No result available.";
            return View("~/Views/Result/View.cshtml", aiResponse);
        }
    }
}