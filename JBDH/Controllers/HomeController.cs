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
            IEmbeddingService embeddingService)
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

        [HttpPost("AnalyzeJobEmbeddings")]
        public async Task<IActionResult> AnalyzeJobEmbeddings(CompareCvToJobAdViewModel model)
        {
            LoggingConfigService.ReturnInfo("Starting job embeddings analysis.");
            
            // Debug logging
            LoggingConfigService.ReturnInfo($"Model received - CvPath is null: {model?.CvPath == null}");
            LoggingConfigService.ReturnInfo($"JobAdText length: {model?.JobAdText?.Length ?? 0}");
            
            if (model?.CvPath == null)
            {
                return BadRequest(new { error = "CV file is required." });
            }
            
            if (string.IsNullOrWhiteSpace(model.JobAdText))
            {
                return BadRequest(new { error = "Job ad text is required." });
            }

            // Save CV file
            var savedCvPath = await FileHelper.SaveUploadedFileAsync(model.CvPath, _environment.WebRootPath);
            
            this.LogAction($"Analyzing job embeddings for CV: {savedCvPath}");

            // Chunk the job ad text
            var jobAdChunks = _textChunkingService.ChunkJobAd(model.JobAdText);
            LoggingConfigService.ReturnInfo($"Job ad chunked into {jobAdChunks.Count} pieces.");

            // Extract CV text for comparison
            var cvText = await _manageIOService.ExtractTextFromFile(savedCvPath ?? string.Empty);
            if (string.IsNullOrWhiteSpace(cvText))
            {
                return BadRequest(new { error = "Could not extract text from CV file." });
            }

            // Extract requirements from each chunk using AI
            var allRequirements = new List<object>();
            for (int i = 0; i < jobAdChunks.Count; i++)
            {
                var chunk = jobAdChunks[i];
                LoggingConfigService.ReturnInfo($"Processing chunk {i + 1}/{jobAdChunks.Count}");

                var aiRequest = new AIRequest
                {
                    CommandId = 6,
                    JobAdText = chunk,
                    User = new User {
                        Id = 1,
                        Email = "test@example.com",
                        PasswordHash = "dummy",
                        Location = "Stockholm"
                    }
                };

                var aiResult = await _aiApiClient.GetAIResponse(aiRequest);
                if (aiResult.Success && !string.IsNullOrWhiteSpace(aiResult.Response))
                {
                    try
                    {
                        // Parse the JSON response to extract requirements as a strongly typed list
                        var chunkRequirements = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Requirement>>(aiResult.Response ?? string.Empty);
                        if (chunkRequirements != null)
                        {
                            allRequirements.Add(new { 
                                chunkIndex = i, 
                                chunkText = chunk.Substring(0, Math.Min(100, chunk.Length)) + "...",
                                requirements = chunkRequirements 
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingConfigService.ReturnError($"Failed to parse requirements from chunk {i}: {ex.Message}");
                    }
                }
            }

            // TODO: Add embedding analysis logic here
            // TODO: Add requirement extraction logic here
            // TODO: Add scoring logic here

            return Ok(new { 
                success = true, 
                message = "Job embeddings analysis completed.", 
                cvPath = savedCvPath, 
                jobAdChunksCount = jobAdChunks.Count,
                cvTextLength = cvText.Length,
                extractedRequirements = allRequirements,
                totalRequirementsFound = allRequirements.Count
            });
        }

        [HttpPost("AnalyzeJobEmbeddingsAlt")]
        public async Task<IActionResult> AnalyzeJobEmbeddingsAlt([FromForm] IFormFile CvPath, [FromForm] string JobAdText)
        {
            LoggingConfigService.ReturnInfo("Starting job embeddings analysis (Alternative method).");
            
            // Debug logging
            LoggingConfigService.ReturnInfo($"File received - is null: {CvPath == null}");
            LoggingConfigService.ReturnInfo($"File name: {CvPath?.FileName ?? "null"}");
            LoggingConfigService.ReturnInfo($"JobAdText length: {JobAdText?.Length ?? 0}");
            
            if (CvPath == null)
            {
                return BadRequest(new { error = "CV file is required." });
            }
            
            if (string.IsNullOrWhiteSpace(JobAdText))
            {
                return BadRequest(new { error = "Job ad text is required." });
            }

            // Save CV file
            var savedCvPath = await FileHelper.SaveUploadedFileAsync(CvPath, _environment.WebRootPath);
            
            this.LogAction($"Analyzing job embeddings for CV: {savedCvPath}");

            return Ok(new { 
                success = true, 
                message = "Alternative method works!", 
                cvPath = savedCvPath, 
                fileName = CvPath.FileName,
                jobAdTextLength = JobAdText.Length
            });
        }

        [HttpGet("TestJobAdExtraction")]
        public async Task<IActionResult> TestJobAdExtraction()
        {
            // Hårdkodad jobbannons
            string jobAdText = """

Senior utvecklare med ledaransvar till etablerat fintech-bolag i Stockholm. 

Jag söker en erfaren ingenjör med erfarenhet som scrum master/projektledare, eller någon som är senior utvecklare och ivrig att ta steget in i en roll där du har praktisk erfarenhet av koden, men också kan ta ett steg mot ledarskap, projektplanering, färdplan och strategi!

Företaget är ett väletablerat produktföretag vars plattform är en användarfokuserad, datadriven motor som hjälper människor att fatta mer välgrundade ekonomiska beslut och låter dem hitta, jämföra och säkra de bästa erbjudandena på lån, bolån, räntor och andra finansiella tjänster, enklare än någonsin tidigare!

I slutändan är deras produkt att hjälpa människor att spara pengar, helt kostnadsfritt! De har funnits i över 10 år, samarbetar med över 40 av Sveriges största banker och finansföretag, är konsekvent lönsamma, men behåller en känsla av ett litet till medelstort företag, och har mycket spännande utveckling på gång.

Hybridroll för senior utvecklare som vill kombinera tekniskt arbete med ledaransvar.

Huvudansvarsområden:
- Skriva ren och underhållbar kod (70-80% av tiden)
- Fungera som Scrum Master och delta i projektplanering
- Vägleda teamet och koordinera över avdelningsgränser
- Delta i code reviews och systemutveckling

Teknisk stack: TypeScript/JavaScript, NodeJS, VueJS, MongoDB, AWS, SQL, Grafana

Vi tror du har erfarenhet av
- Minst 5 års erfarenhet av den tekniska stacken
- Erfarenhet av Agile/Scrum, helst som Scrum Master
- Intresse för ledarskap och projektplanering
- Flytande engelska och arbetstillstånd i Sverige

Extra meriterande

- Umbraco, Episerver / Optimizely
- Bra koll på Devops-relaterade verktyg och processer, Nginx, Docker, CI-lösningar osv.
- Wordpress

Om företaget
- Etablerat fintech-bolag (10+ år)
- Hjälper användare jämföra lån och hypotekslån gratis
- Samarbetar med 40+ svenska banker
- Platt hierarki med 12 personer i tech-teamet
- Hybridarbete med kontor i Stockholm city
""";
            // Hårdkodad CV-path
            string cvPath = @"C:\\Users\\yazda\\Desktop\\JBDH_V0.1\\JBDH\\wwwroot\\uploads\\Hossein_YazdaniPajouh_CV.pdf";

            // Chunka jobbannonsen
            var jobAdChunks = _textChunkingService.ChunkJobAd(jobAdText);
            LoggingConfigService.ReturnInfo($"Job ad chunked into {jobAdChunks.Count} pieces.");

            // Extrahera krav från varje chunk
            var allRequirements = new List<object>();
            var aiRawResponses = new List<string>();
            for (int i = 0; i < jobAdChunks.Count; i++)
            {
                var chunk = jobAdChunks[i];
                LoggingConfigService.ReturnInfo($"Processing chunk {i + 1}/{jobAdChunks.Count}");
                LoggingConfigService.ReturnInfo($"Chunk text: {chunk.Substring(0, Math.Min(100, chunk.Length))}...");

                var aiRequest = new AIRequest
                {
                    CommandId = 6,
                    JobAdText = chunk,
                    User = new User {
                        Id = 1,
                        Email = "test@example.com",
                        PasswordHash = "dummy",
                        Location = "Stockholm"
                    }
                };

                var aiResult = await _aiApiClient.GetAIResponse(aiRequest);
                LoggingConfigService.ReturnInfo($"AI success: {aiResult.Success}, AI raw response: {aiResult.Response}");
                aiRawResponses.Add(aiResult.Response ?? "<null>");
                if (aiResult.Success && !string.IsNullOrWhiteSpace(aiResult.Response))
                {
                    try
                    {
                        // Parse the JSON response to extract requirements as a strongly typed list
                        var chunkRequirements = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Requirement>>(aiResult.Response ?? string.Empty);
                        if (chunkRequirements != null)
                        {
                            allRequirements.Add(new {
                                chunkIndex = i,
                                chunkText = chunk.Substring(0, Math.Min(100, chunk.Length)) + "...",
                                requirements = chunkRequirements
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingConfigService.ReturnError($"Failed to parse requirements from chunk {i}: {ex.Message}");
                    }
                }
            }

            // Extrahera CV-text (valfritt, men visar att det funkar)
            var cvText = await _manageIOService.ExtractTextFromFile(cvPath);
            LoggingConfigService.ReturnInfo($"CV text length: {cvText?.Length ?? 0}");

            return Ok(new {
                jobAdChunksCount = jobAdChunks.Count,
                aiRawResponses = aiRawResponses,
                extractedRequirements = allRequirements,
                cvTextLength = cvText?.Length ?? 0
            });
        }

        [HttpGet("TestEmbedding")]
        public async Task<IActionResult> TestEmbedding([FromQuery] string text = "Senior developer with 5 years experience")
        {
            LoggingConfigService.ReturnInfo("Starting embedding test...");
            
            try
            {
                // Test 1: Single embedding
                LoggingConfigService.ReturnInfo($"Generating embedding for: {text}");
                var embedding = await _embeddingService.GetEmbeddingAsync(text);
                
                LoggingConfigService.ReturnInfo($"Successfully generated embedding with {embedding.Length} dimensions");

                // Test 2: Batch embeddings
                var testTexts = new List<string>
                {
                    "Python developer",
                    "Senior backend engineer",
                    "Full stack developer"
                };

                LoggingConfigService.ReturnInfo("Generating batch embeddings...");
                var batchEmbeddings = await _embeddingService.GetEmbeddingsBatchAsync(testTexts);
                LoggingConfigService.ReturnInfo($"Successfully generated {batchEmbeddings.Count} batch embeddings");

                // Test 3: Similarity calculation
                if (batchEmbeddings.Count >= 2)
                {
                    var similarity = _embeddingService.CalculateSimilarity(embedding, batchEmbeddings[0]);
                    LoggingConfigService.ReturnInfo($"Similarity between embeddings: {similarity:F4}");
                }

                return Ok(new
                {
                    success = true,
                    message = "Embedding test successful",
                    singleEmbedding = new
                    {
                        text = text,
                        dimensions = embedding.Length,
                        sampleValues = embedding.Take(5).ToArray()
                    },
                    batchEmbeddings = new
                    {
                        count = batchEmbeddings.Count,
                        texts = testTexts,
                        dimensions = batchEmbeddings.Count > 0 ? batchEmbeddings[0].Length : 0
                    },
                    note = "Full embedding vectors are not returned for brevity. API working correctly!"
                });
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Embedding test failed: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}