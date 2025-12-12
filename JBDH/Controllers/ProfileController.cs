using JBDH.Helpers;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Friendship;
using JBDH.Models.Core;
using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JBDH.Models.Job;
using Microsoft.AspNetCore.Cors;
using JBDH.Services.Configuration;
using JBDH.Models.DTOs;


namespace JBDH.Controllers
{
    [Authorize]
    [IncludeInApi]
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowLocalFrontend")]
    public class ProfileController : Controller
    {
    private readonly ApplicationDbContext _context;
    private readonly IProfileService _profileService;
    private readonly IFriendService _friendService;
    private readonly IAIApiClient _AIApiClient;
    private readonly IUserAIResponseService _userAIResponseService;
    private readonly IUserPropertiesService _userPropertiesService;
    private readonly JBDH.Services.Helpers.IManageIOService _manageIOService;

        public ProfileController(
            IUserAIResponseService userAIResponseService,
            IUserPropertiesService userPropertiesService,
            ApplicationDbContext context,
            IProfileService profileService,
            IFriendService friendService,
            IAIApiClient aIAApiClient,
            JBDH.Services.Helpers.IManageIOService manageIOService)
        {
            _context = context;
            _profileService = profileService;
            _friendService = friendService;
            _AIApiClient = aIAApiClient;
            _userAIResponseService = userAIResponseService;
            _userPropertiesService = userPropertiesService;
            _manageIOService = manageIOService;
        }

        [HttpPost("AnalyzeCV")]
        public async Task<IActionResult> AnalyzeCV([FromForm] IFormFile cvFile)
        {
            LoggingConfigService.ReturnInfo("AnalyzeCV endpoint called.");
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return Unauthorized();

            if (!authResult.Value.HasValue)
                return BadRequest("Authenticated user ID is missing.");

            if (cvFile == null || cvFile.Length == 0)
                return BadRequest("No CV file provided.");

            var result = await _userPropertiesService.ChangeCVAndAnalyzeAsync(cvFile, authResult.Value.Value);
            if (!result.Success)
                return BadRequest(result.ErrorMessage ?? "Failed to upload or analyze CV.");

            // Hämta och returnera alla AI-respons för användaren som DTOs
            var aiResponseDtos = await _userAIResponseService.GetAllUserAIResponseDtosAsync(authResult.Value.Value);
            return Ok(new { success = true, message = "CV uploaded and analysis completed.", aiResponses = aiResponseDtos });
        }

        
        [HttpGet("getprofile/{userId:int}")]
        public async Task<IActionResult> GetProfile(int userId)
            {
                var authResult = this.EnsureAuthenticatedUserOrRedirect();
                if (authResult.ErrorResult != null)
                    return authResult.ErrorResult;

                if (authResult.Value.HasValue && authResult.Value.Value == userId)
                    return this.RedirectToMyProfile();

                if (!authResult.Value.HasValue)
                    return this.ErrorPage("Authenticated user ID is missing.");

                try
                {
                    var viewModel = await _profileService.GetProfileViewModelAsync(authResult.Value.Value, userId);
                    var currentUser = await _context.Users.FindAsync(authResult.Value.Value);
                    ViewBag.CurrentUser = currentUser;
                    return View(viewModel);
                }
                catch (Exception ex)
                {
                    return this.ErrorPage(ex.Message);
                }
            }
            
        [HttpGet("getmyprofile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            var aiResponseDtos = await _userAIResponseService.GetAllUserAIResponseDtosAsync(user.Id);

            var userDto = new UserDto
            {
                Id = user.Id,
                PasswordHash = user.PasswordHash,
                Email = user.Email,
                Location = user.Location,
                Name = user.Name,
                DesiredEmploymentTypes = user.DesiredEmploymentTypes,
                DesiredWorkModes = user.DesiredWorkModes,
                PhoneNumber = user.PhoneNumber,
                TravelAvailability = user.TravelAvailability,
                AvailableFrom = user.AvailableFrom,
                DesiredCity = user.DesiredCity,
                WorkPlace = user.WorkPlace,
                Competencies = user.Competencies,
                CVPath = user.CVPath,
                DeveloperMode = user.DeveloperMode,
                Searchable = user.Searchable,
                IsBanned = user.IsBanned,
                //SelectedRoles = user.Roles?.Select(r => r.RoleName).ToList(),
                SentFriendRequests = user.SentFriendRequests?.ToList(),
                ReceivedFriendRequests = user.ReceivedFriendRequests?.ToList(),
                JobApplications = user.JobApplications?.ToList(),
                DataFlows = user.DataFlows?.ToList(),
                AIResponses = aiResponseDtos,
                Roles = user.Roles?.ToList(),
                ManagedOrganisations = user.ManagedOrganisations?.ToList(),
                OwnedUsers = user.OwnedUsers?.Select(u => new UserDto { Id = u.Id, Name = u.Name }).ToList(),
                Owner = user.Owner != null ? new UserDto { Id = user.Owner.Id, Name = user.Owner.Name } : null,
                OwnerId = user.OwnerId,
                AboutMe = user.AboutMe
            };
            return Ok(userDto);
        }

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(int addresseeId)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect("User is not authenticated.");
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            var parseResult = this.ValidateAndHandleParseError(addresseeId.ToString(), "Invalid addressee ID.");
            if (parseResult.ErrorResult != null)
                return parseResult.ErrorResult;

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");
            if (!parseResult.Value.HasValue)
                return this.ErrorPage("Parsed addressee ID is missing.");

            var result = await _friendService.SendFriendRequestAsync(authResult.Value.Value, parseResult.Value.Value);
            if (result.Success)
            {
                return this.SetSuccessAndRedirect("Friend request sent!", "Profile", new { userId = addresseeId });
            }
            else
            {
                return this.ErrorPage(result.ErrorMessage ?? "Could not send friend request.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            await _friendService.AcceptFriendRequestAsync(friendshipId);
            return this.SetSuccessAndRedirect("Friend request accepted!", "MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineFriendRequest(int friendshipId)
        {
            await _friendService.DeclineFriendRequestAsync(friendshipId);
            return this.SetSuccessAndRedirect("Friend request declined.", "MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateJobApplicationNotes(int applicationId, string notes)
        {
            var app = await _context.Set<JobApplication>().FindAsync(applicationId);
            if (app == null)
                return this.ErrorPage("Job application not found.");

            app.Notes = notes;
            await _context.SaveChangesAsync();
            return this.SetSuccessAndRedirect("Notes updated!", "MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeveloperToolDeleteUserFromSystem(int userId)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return Unauthorized();

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            var currentUser = await _context.Users.FindAsync(authResult.Value.Value);
            if (currentUser == null || currentUser.DeveloperMode != true)
                return this.ForbidWithMessage("You do not have permission to perform this action.");

            if (currentUser.Id == userId)
                return this.BadRequestMessage("You cannot delete yourself from this action.");

            var userToDelete = await _context.Users.FindAsync(userId);
            if (userToDelete == null)
                return this.ErrorPage("User not found.");

            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();

            return this.SetSuccessAndRedirect("User deleted successfully.", "Index", new { controller = "Home" });
        }
        
        [HttpGet("LatestAIUserResponse")]
        public async Task<IActionResult> LatestAIUserResponse()
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return Unauthorized();

            if (!authResult.Value.HasValue)
                return BadRequest("Authenticated user ID is missing.");

            var aiUserResponse = await _userAIResponseService.GetLatestUserAIResponseAsync(authResult.Value.Value);
            if (aiUserResponse == null)
                return NotFound(new { error = "No AI analysis found for this user." });

            // Clean and robustly deserialize the AI response (handle double-encoded JSON)
            JBDH.Models.User.CVAnalysisResult? analysisObj = null;
            var raw = aiUserResponse.ResponseText ?? string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    // Clean code block markers if present
                    var cleaned = _manageIOService.ExtractJsonFromCodeBlock(raw);
                    JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[ProfileController] Cleaned AI JSON: {cleaned}");
                    string? jsonToParse = cleaned;
                    if ((jsonToParse.StartsWith("\"") && jsonToParse.EndsWith("\"")) || jsonToParse.Contains("\\n") || jsonToParse.Contains("\\\""))
                    {
                        jsonToParse = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(jsonToParse);
                        JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[ProfileController] Unescaped AI JSON: {jsonToParse}");
                    }
                    if (!string.IsNullOrEmpty(jsonToParse))
                        analysisObj = Newtonsoft.Json.JsonConvert.DeserializeObject<JBDH.Models.User.CVAnalysisResult>(jsonToParse);
                    JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[ProfileController] Parsed CVAnalysisResult: {Newtonsoft.Json.JsonConvert.SerializeObject(analysisObj)}");
                }
            }
            catch (Exception ex)
            {
                JBDH.Services.Configuration.LoggingConfigService.ReturnError($"[ProfileController] Failed to parse AI analysis: {ex.Message}");
            }
            var dto = new JBDH.Models.DTOs.UserAIResponseDto
            {
                Id = aiUserResponse.Id,
                UserId = aiUserResponse.UserId,
                ResponseText = raw,
                CreatedAt = aiUserResponse.CreatedAt,
                Analysis = analysisObj
            };
            return Ok(dto);
        }
    }
}