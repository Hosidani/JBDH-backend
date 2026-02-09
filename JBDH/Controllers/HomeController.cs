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
using Newtonsoft.Json;
using JBDH.Models.AI;
using JBDH.Services.Helpers;
using Newtonsoft.Json.Linq;
using JBDH.Models.User;

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
        private readonly ITextChunkingService _textChunkingService;
        private readonly IEmbeddingService _embeddingService;
        private readonly ICVAnalysisService _cvAnalysisService;
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
            IAIApiClient aiApiClient,
            ITextChunkingService textChunkingService,
            IEmbeddingService embeddingService,
            ICVAnalysisService cvAnalysisService)
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
            _textChunkingService = textChunkingService;
            _embeddingService = embeddingService;
            _cvAnalysisService = cvAnalysisService;
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
        public async Task<IActionResult> CompareCvToJobAd(CompareCvToJobAdViewModel model, string language = "English")
        {
            _logger.LogInformation("CompareCvToJobAd called. Model: {@Model}", model);
            if (model?.CvPath == null)
            {
                _logger.LogWarning("No CV file uploaded.");
                return BadRequest(new { error = "CV file is required." });
            }

            var savedCvPath = await FileHelper.SaveUploadedFileAsync(model.CvPath, _environment.WebRootPath);
            if (string.IsNullOrEmpty(savedCvPath))
            {
                return BadRequest(new { error = "File could not be saved." });
            }

            string cvText;
            try
            {
                cvText = FileHelper.ExtractText(savedCvPath);
                _logger.LogInformation("Extracted CV text length: {Length}", cvText?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during CV text extraction.");
                return BadRequest(new { error = "CV text extraction error: " + ex.Message });
            }

            if (string.IsNullOrWhiteSpace(cvText))
            {
                _logger.LogWarning("CV text is empty or null.");
                return BadRequest(new { error = "CV text extraction failed or is empty." });
            }

            var aiRequest = new AIRequest
            {
                CommandId = 5,
                CVPath = savedCvPath,
                JobAdText = model.JobAdText,
                User = null
            };

            var aiResult = await _aiApiClient.GetAIResponse(aiRequest);
            var aiResp = aiResult.Response ?? string.Empty;
            _logger.LogInformation("AI response: {AIResp}", aiResp);

            object? parsed = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(aiResp))
                    parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(aiResp);
            }
            catch { }

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



        [HttpPost("CompareCvToJobAdV2")]
        public async Task<IActionResult> CompareCvToJobAdV2(CompareCvToJobAdViewModel model, string language = "English")
        {
            if (model?.CvPath == null)
                return BadRequest("CV file is required");

            if (string.IsNullOrWhiteSpace(model.JobAdText))
                return BadRequest("Job ad text is required");

            try
            {
                // 1. Extract CV text
                var savedPath = await FileHelper.SaveUploadedFileAsync(
                    model.CvPath,
                    _environment.WebRootPath
                );

                var cvText = await _manageIOService.ExtractTextFromFile(savedPath ?? "");
                if (string.IsNullOrWhiteSpace(cvText))
                    return BadRequest("Could not extract CV text");

                // Extract CV profession and requirements using service
                var cvExtractionResult = await _cvAnalysisService.ExtractCVProfessionAndRequirementsAsync(cvText);
                if (cvExtractionResult == null)
                {
                    _logger.LogWarning("CV extraction failed or returned null");
                    return BadRequest("CV extraction failed");
                }

                var cvProfession = cvExtractionResult.Profession;
                var cvRequirements = cvExtractionResult.Requirements;

                // Logging and extraction for CareerSummaryPitch
                LoggingConfigService.ReturnInfo("cvText: " + cvText);
                var cvjobpitch = await _jobAIAnalysisService.GetCvJobComparisonAsync(model.JobAdText, savedPath ?? "");
                _logger.LogInformation("cvjobpitch raw response: {Pitch}", cvjobpitch);

                string? pitch = null;
                RequirementExtractionResponse? extraction = null;
                if (!string.IsNullOrWhiteSpace(cvjobpitch))
                {
                    var clean = cvjobpitch.Trim();
                    _logger.LogInformation("Cleaned AI response before JSON parse: {Cleaned}", clean);
                    if (clean.StartsWith("```json"))
                        clean = clean.Substring(7).Trim();
                    if (clean.EndsWith("```") && clean.Length > 3)
                        clean = clean.Substring(0, clean.Length - 3).Trim();

                    _logger.LogInformation("Final cleaned JSON string: {Cleaned}", clean);

                    if (clean.StartsWith("{"))
                    {
                        try
                        {
                            extraction = JsonConvert.DeserializeObject<RequirementExtractionResponse>(clean);
                            _logger.LogInformation("extraction object: {@Extraction}", extraction);
                            pitch = extraction?.CareerSummaryPitch;
                            _logger.LogInformation("CareerSummaryPitch extracted: {Pitch}", pitch);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize cvjobpitch");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("cvjobpitch is not valid JSON after cleaning: {Pitch}", clean);
                    }
                }
                else
                {
                    _logger.LogWarning("cvjobpitch is not valid JSON: {Pitch}", cvjobpitch);
                }
                cvExtractionResult.CareerSummaryPitch = pitch;
                if (string.IsNullOrWhiteSpace(pitch))
                {
                    _logger.LogWarning("CareerSummaryPitch could not be extracted or is empty.");
                    return BadRequest("CareerSummaryPitch could not be extracted from AI response.");
                }
                
                // 2. Extract job requirements and profession using service
                var (requirements, jobProfession) = await _jobAIAnalysisService.ExtractJobRequirementsAndProfessionAsync(model.JobAdText);

                // Begränsa till max 100 requirements för att undvika Gemini rate limit
                if (requirements.Count > 100)
                {
                    _logger.LogWarning($"Antal requirements ({requirements.Count}) överskrider 100. Endast de första 100 används för embedding.");
                    requirements = requirements.Take(100).ToList();
                }

                if (requirements.Count == 0)
                    return BadRequest("No comparable requirements found after processing");

                // 3. Compare skills using embedding service
                var (matchPercentage, results) = await _embeddingService.CompareSkillsWithEmbeddingsAsync(cvRequirements, requirements);

                // Calculate profession score with embeddings
                _logger.LogInformation("Calculating profession score - CV: {CvMain}/{CvSub}/{CvSpecialty} vs Job: {JobMain}/{JobSub}/{JobSpecialty}", 
                    cvProfession?.Main, cvProfession?.Sub, cvProfession?.Specialty, 
                    jobProfession?.Main, jobProfession?.Sub, jobProfession?.Specialty);
                
                var professionScore = await _embeddingService.CalculateProfessionScoreWithEmbeddingsAsync(cvProfession, jobProfession);
                var professionPercentage = (float)Math.Round(professionScore * 100, 2);
                
                _logger.LogInformation("Final profession score: {ProfessionScore} ({ProfessionPercentage}%)", 
                    professionScore, professionPercentage);

                // Calculate total score using weighted combination (NOT multiplication!)
                // Skills (30% weight) + Profession (70% weight) - profession is more important for job fit
                var totalMatchScore = (matchPercentage / 100f * 0.3f) + (professionScore * 0.7f);
                var totalMatchPercentage = (float)Math.Round(totalMatchScore * 100, 2);
                
                _logger.LogInformation("Total score calculation: Skills {SkillsPercent}% (30% weight) + Profession {ProfessionPercent}% (70% weight) = {TotalPercent}%", 
                    matchPercentage, professionPercentage, totalMatchPercentage);

                return Ok(new
                {
                    totalScore = totalMatchPercentage,
                    skillsScore = matchPercentage,
                    professionScore = professionPercentage,
                    cvProfession,
                    jobProfession,
                    matches = results,
                    totalRequirements = requirements.Count,
                    careerSummaryPitch = cvExtractionResult.CareerSummaryPitch
                });
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError(ex.Message);
                return BadRequest("Comparison failed");
            }
        }
    }
            
}
