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

            return Ok(new { success = true, message = "CV uploaded and analysis started." });
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

    [HttpGet("profile/{userId:int}")]
    public async Task<IActionResult> Profile(int userId)
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

    [HttpGet("profile/me")]
    public async Task<IActionResult> MyProfile()
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return Unauthorized();

            if (!authResult.Value.HasValue)
                return BadRequest("Authenticated user ID is missing.");

            var model = await _profileService.GetMyProfileViewModelAsync(authResult.Value.Value);
            var latestProposal = await _context.AIUserCourseProposals
                .Where(p => p.UserId == authResult.Value.Value)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            // Map to DTO and parse AI fields


            object? cvAnalysis = null;
            var cvRaw = model.UserAIResponse?.ResponseText;
            if (!string.IsNullOrWhiteSpace(cvRaw))
            {
                try
                {
                    // Log the raw JSON for debugging
                    JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[ProfileController] Raw CVAnalysis JSON: {cvRaw}");
                    var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<JBDH.Models.User.CVAnalysisResult>(cvRaw);
                    // Log the parsed object for debugging
                    JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"[ProfileController] Parsed CVAnalysisResult: {Newtonsoft.Json.JsonConvert.SerializeObject(parsed)}");
                    cvAnalysis = _manageIOService.ToCamelCaseObject(parsed);
                }
                catch (Exception ex)
                {
                    JBDH.Services.Configuration.LoggingConfigService.ReturnError($"[ProfileController] Failed to parse CVAnalysisResult: {ex.Message}");
                    cvAnalysis = null;
                }
            }

            object? courseProposals = null;
            var courseRaw = latestProposal?.ProposalJson;
            try
            {
                if (!string.IsNullOrWhiteSpace(courseRaw))
                {
                    var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(courseRaw);
                    courseProposals = _manageIOService.ToCamelCaseObject(parsed);
                }
            }
            catch { }

            var dto = new JBDH.Models.DTOs.ProfileDto
            {
                User = new JBDH.Models.DTOs.UserDto
                {
                    Id = model.User.Id,
                    Name = model.User.Email, // Or use a real name property if available
                    Email = model.User.Email,
                    AboutMe = model.User.AboutMe
                    // Add other user fields as needed
                },
                IsFriend = model.IsFriend,
                Friends = model.Friends.Select(f => new JBDH.Models.DTOs.FriendDto { Id = f.Id, UserName = f.Email }).ToList(),
                PendingFriendRequests = model.PendingFriendRequests.Select(fr => new JBDH.Models.DTOs.FriendRequestDto { Id = fr.Id, RequesterName = fr.Requester?.Email }).ToList(),
                JobApplications = model.JobApplications.Select(j => new JBDH.Models.DTOs.JobApplicationDto {
                    ApplicationId = j.ApplicationId,
                    JobId = j.JobId,
                    JobTitle = j.JobTitle,
                    JobUrl = j.JobUrl,
                    JobDescription = j.JobDescription,
                    Notes = j.Notes,
                    AppliedAt = j.AppliedAt
                }).ToList(),
                CVAnalysisJson = cvRaw,
                CVAnalysis = cvAnalysis,
                CourseProposalsJson = courseRaw,
                CourseProposals = courseProposals
            };
            return Ok(dto);
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
        public async Task<IActionResult> UpdateAboutMe(string aboutMe)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            var user = await _context.Users.FindAsync(authResult.Value.Value);
            if (user == null)
                return this.ErrorPage("User not found.");

            user.AboutMe = aboutMe;
            await _context.SaveChangesAsync();

            return this.SetSuccessAndRedirect("About Me updated!", "MyProfile");
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
        public async Task<IActionResult> DeleteUserFromProfile(int userId)
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

        [HttpGet]
        public async Task<IActionResult> GetMyProfile()
        {
            LoggingConfigService.ReturnInfo("[ProfileController] Entered GetMyProfile endpoint.");
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return Unauthorized();

            if (!authResult.Value.HasValue)
                return BadRequest("Authenticated user ID is missing.");

            var model = await _profileService.GetMyProfileViewModelAsync(authResult.Value.Value);
            var latestProposal = await _context.AIUserCourseProposals
                .Where(p => p.UserId == authResult.Value.Value)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            object? cvAnalysis = null;
            var cvRaw = model.UserAIResponse?.ResponseText;
            try
            {
                if (!string.IsNullOrWhiteSpace(cvRaw))
                    cvAnalysis = Newtonsoft.Json.JsonConvert.DeserializeObject(cvRaw);
            }
            catch { }

            object? courseProposals = null;
            var courseRaw = latestProposal?.ProposalJson;
            try
            {
                if (!string.IsNullOrWhiteSpace(courseRaw))
                    courseProposals = Newtonsoft.Json.JsonConvert.DeserializeObject(courseRaw);
            }
            catch { }

            var dto = new JBDH.Models.DTOs.ProfileDto
            {
                User = new JBDH.Models.DTOs.UserDto
                {
                    Id = model.User.Id,
                    Name = model.User.Email, // Or use a real name property if available
                    Email = model.User.Email,
                    AboutMe = model.User.AboutMe
                    // Add other user fields as needed
                },
                IsFriend = model.IsFriend,
                Friends = model.Friends.Select(f => new JBDH.Models.DTOs.FriendDto { Id = f.Id, UserName = f.Email }).ToList(),
                PendingFriendRequests = model.PendingFriendRequests.Select(fr => new JBDH.Models.DTOs.FriendRequestDto { Id = fr.Id, RequesterName = fr.Requester?.Email }).ToList(),
                JobApplications = model.JobApplications.Select(j => new JBDH.Models.DTOs.JobApplicationDto {
                    ApplicationId = j.ApplicationId,
                    JobId = j.JobId,
                    JobTitle = j.JobTitle,
                    JobUrl = j.JobUrl,
                    JobDescription = j.JobDescription,
                    Notes = j.Notes,
                    AppliedAt = j.AppliedAt
                }).ToList(),
                CVAnalysisJson = cvRaw,
                CVAnalysis = cvAnalysis,
                CourseProposalsJson = courseRaw,
                CourseProposals = courseProposals
            };
            return Ok(dto);
        }
    }
}