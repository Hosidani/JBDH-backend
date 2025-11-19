using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
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
    [IncludeInApi]
    [ApiController]
    [Route("api/[controller]")]
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
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ILogger<HomeController> logger,
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
            _logger = logger;
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
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (this.IsAuthenticated())
            {
                var authResult = this.EnsureAuthenticatedUserOrRedirect();
                if (authResult.ErrorResult != null)
                    // For API clients, return 401 so frontend can handle redirect/login
                    return Unauthorized();

                if (!authResult.Value.HasValue)
                    return Unauthorized();

                var userId = authResult.Value.Value;
                var userExists = await this.UserExistsAsync(_context, userId);
                if (!userExists)
                {
                    await this.SignOutAndRedirectToHomeAsync();
                    return Unauthorized();
                }
            }

            var dataFlows = _dataFlowService.GetAllRecursive();
            var listDataFlowViewModel = new ListDataFlowViewModel { DataFlows = dataFlows };
            return Ok(listDataFlowViewModel);
        }

        [Authorize]
        [HttpDelete("DeleteDataFlow/{id}")]
        public IActionResult DeleteDataFlow(int id)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect("User is not authenticated.");
            if (authResult.ErrorResult != null)
                return Unauthorized();

            this.LogAction($"Deleting data flow for user ID: {authResult.Value}");
            _dataFlowService.DeleteDataFlow(id);
            return Ok(new { success = true });
        }

        [HttpGet("CreateFlow")]
        public IActionResult CreateFlow()
        {
            // Frontend will render the create form; return a minimal payload
            return Ok(new { message = "ok" });
        }

        [Authorize]
        [HttpPost("CreateFlow")]
        public async Task<IActionResult> CreateFlow(DataFlowViewModel model)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect("User is not authenticated.");
            if (authResult.ErrorResult != null)
                return Unauthorized();

            var userId = authResult.Value;
            this.LogAction($"Creating new data flow with title: {model.Content}");

            if (!ModelState.IsValid)
            {
                this.SetModelInvalidError("Model state is invalid.");
                return BadRequest(ModelState);
            }
            if (userId.HasValue && userId.Value > 0)
            {
                if (string.IsNullOrEmpty(model.Content))
                {
                    this.SetEmptyFieldError("Content");
                    return BadRequest(new { error = "Content cannot be empty." });
                }
                await _dataFlowService.CreateAsync(model, userId.Value);
            }
            return Ok(new { success = true });
        }

        [HttpGet("UserPosts/{userId}")]
        public IActionResult UserPosts(int userId)
        {
            var posts = _dataFlowService.GetByUserId(userId);
            return Ok(posts);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet("ErrorPage")]
        public IActionResult ErrorPage()
        {
            var message = "An unexpected error occurred. Please try again later.";
            this.SetErrorMessage(message);
            this.LogAction($"Error page: {message}");
            return Problem(detail: message);
        }

        [HttpGet("Settings")]
        public IActionResult Settings()
        {
            // Return minimal settings payload; frontend should render settings UI
            return Ok(new { message = "settings" });
        }

        [HttpPost("ReportDataFlow")]
        public async Task<IActionResult> ReportDataFlow(int reportedUserId, int dataFlowId, string dataFlowText, string reason)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect("User is not authenticated.");
            if (authResult.ErrorResult != null)
                return Unauthorized();

            if (!authResult.Value.HasValue)
                return Unauthorized();

            var reporterUserId = authResult.Value.Value;
            var reporterExists = await this.UserExistsAsync(_context, reporterUserId);
            var reportedExists = await this.UserExistsAsync(_context, reportedUserId);
            if (!reporterExists || !reportedExists)
            {
                this.LogAction($"Reporter or reported user does not exist. Reporter ID: {reporterUserId}, Reported User ID: {reportedUserId}");
                return Ok(new { success = true, message = "The post has been reported." });
            }

            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, dataFlowId, dataFlowText);
            if (!result.Success)
            {
                return BadRequest(new { success = false, error = result.ErrorMessage ?? "An error occurred while reporting the post." });
            }

            return Ok(new { success = true, message = "The post has been reported." });
        }

        [Authorize]
        [HttpPost("CommentOnDataFlow")]
        public async Task<IActionResult> CommentOnDataFlow(int dataFlowId, string comment)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return Unauthorized();

            if (!authResult.Value.HasValue)
                return Unauthorized();

            var userId = authResult.Value.Value;
            if (string.IsNullOrWhiteSpace(comment))
            {
                this.SetEmptyFieldError("Comment");
                return BadRequest(new { error = "Comment cannot be empty." });
            }

            await _dataFlowService.AddCommentAsync(dataFlowId, userId, comment);
            return Ok(new { success = true });
        }

        [HttpPost("CompareCvToJobAd")]
        public async Task<IActionResult> CompareCvToJobAd(CompareCvToJobAdViewModel model)
        {
            _logger.LogInformation("[TEST] CompareCvToJobAd anropad! Modell: {@Model}", model);
            LoggingConfigService.ReturnInfo("Starting CV to Job Ad comparison process.");
            if (model?.CvPath == null)
            {
                _logger.LogWarning("[TEST] Ingen CV bifogad i CompareCvToJobAd!");
                return BadRequest(new { error = "CV file is required." });
            }

            var savedCvPath = await FileHelper.SaveUploadedFileAsync(model.CvPath, _environment.WebRootPath);

            var aiRequest = new AIRequest
            {
                CommandId = 5,
                CVPath = savedCvPath,
                JobAdText = model.JobAdText,
                User = null
            };

            var aiResult = await _aiApiClient.GetAIResponse(aiRequest);

            var aiResp = aiResult.Response ?? string.Empty;
            _logger.LogInformation("[TEST] AI-respons: {AIResp}", aiResp);
            LoggingConfigService.ReturnInfo(aiResp);

            object? parsed = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(aiResp))
                    parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(aiResp);
            }
            catch { /* ignore parse errors, return null */ }

            // Return both raw and parsed AI response
            return Ok(new { aiResponse = aiResp, analysis = parsed });
        }

        [HttpGet("Result")]
        public IActionResult Result()
        {
            LoggingConfigService.ReturnInfo("Accessing Result action in HomeController.");
            var aiResponse = TempData["AIResult"] as string ?? "No result available.";
            object? parsed = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(aiResponse) && aiResponse != "No result available.")
                    parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(aiResponse);
            }
            catch { /* ignore parse errors, return null */ }
            return Ok(new { aiResponse, analysis = parsed });
        }
    }
}