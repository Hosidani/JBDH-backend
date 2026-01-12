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
using JBDH.Models.Friendship;
using JBDH.Models.Dataflow;
using JBDH.Models.User;
using FriendshipModel = JBDH.Models.Friendship.Friendship;


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
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            if (user.Id == userId)
                return BadRequest("Use getmyprofile endpoint for your own profile");

            try
            {
                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null)
                    return NotFound("User not found");

                //if (targetUser.Searchable != true)
                //    return Forbid("This user profile is not public");

                var userDto = new
                {
                    Id = targetUser.Id,
                    Name = targetUser.Name,
                    Email = targetUser.Email,
                    Location = targetUser.Location,
                    AboutMe = targetUser.AboutMe,
                    HasCV = !string.IsNullOrEmpty(targetUser.CVPath),
                    CreatedAt = targetUser.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                };
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error getting profile for user {userId}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the profile");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(string name, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name parameter is required");

            if (pageSize > 50)
                pageSize = 50; // Limit page size for performance

            var users = await _context.Users
                .Where(u => (u.Name != null && u.Name.Contains(name)) || u.Email.Contains(name))
                .Where(u => u.Searchable == true) // Only return searchable users
                .Select(u => new {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Location,
                    u.CVPath,
                    HasCV = !string.IsNullOrEmpty(u.CVPath)
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await _context.Users
                .Where(u => (u.Name != null && u.Name.Contains(name)) || u.Email.Contains(name))
                .Where(u => u.Searchable == true)
                .CountAsync();

            var result = new
            {
                users = users,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(result);
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
                Role = user.Role, 
                SelectedRoles = user.Roles?.Select(r => r.Role).ToList(), 
                SentFriendRequests = new List<FriendshipModel>(), // Avoid circular references
                ReceivedFriendRequests = new List<FriendshipModel>(), // Avoid circular references  
                JobApplications = new List<JobApplication>(), // Avoid circular references
                DataFlows = new List<DataFlow>(), // Avoid circular references
                AIResponses = aiResponseDtos,
                Roles = new List<UserRoleAssignment>(), // Remove circular references - use SelectedRoles instead
                ManagedOrganisations = new List<JBDH.Models.Organisation.Organisation>(), // Avoid circular references
                OwnedUsers = user.OwnedUsers?.Select(u => new UserDto { Id = u.Id, Name = u.Name }).ToList(),
                Owner = user.Owner != null ? new UserDto { Id = user.Owner.Id, Name = user.Owner.Name } : null,
                OwnerId = user.OwnerId,
                AboutMe = user.AboutMe,
                CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            };
            return Ok(userDto);
        }

        [HttpPost("sendfriendrequest/{addresseeId:int}")]
        public async Task<IActionResult> SendFriendRequest(int addresseeId)
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            if (addresseeId <= 0)
                return BadRequest("Invalid addressee ID");

            var result = await _friendService.SendFriendRequestAsync(user.Id, addresseeId);
            if (result.Success)
            {
                return Ok(new { message = "Friend request sent successfully" });
            }
            else
            {
                return BadRequest(result.ErrorMessage ?? "Could not send friend request");
            }
        }

        [HttpPost("acceptfriendrequest/{friendshipId:int}")]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            try
            {
                await _friendService.AcceptFriendRequestAsync(friendshipId);
                return Ok(new { message = "Friend request accepted successfully" });
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error accepting friend request {friendshipId}: {ex.Message}");
                return BadRequest("Could not accept friend request");
            }
        }

        [HttpPost("declinefriendrequest/{friendshipId:int}")]
        public async Task<IActionResult> DeclineFriendRequest(int friendshipId)
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            try
            {
                await _friendService.DeclineFriendRequestAsync(friendshipId);
                return Ok(new { message = "Friend request declined successfully" });
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error declining friend request {friendshipId}: {ex.Message}");
                return BadRequest("Could not decline friend request");
            }
        }

        [HttpPost("updatejobapplicationnotes/{applicationId:int}")]
        public async Task<IActionResult> UpdateJobApplicationNotes(int applicationId, [FromBody] UpdateNotesRequest request)
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            var app = await _context.Set<JobApplication>()
                .FirstOrDefaultAsync(ja => ja.ApplicationId == applicationId && ja.UserId == user.Id);
            if (app == null)
                return NotFound("Job application not found or access denied");

            app.Notes = request.Notes ?? string.Empty;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Notes updated successfully" });
        }

        [HttpDelete("developer/deleteuser/{userId:int}")]
        public async Task<IActionResult> DeveloperToolDeleteUserFromSystem(int userId)
        {
            var currentUser = await _userPropertiesService.GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized();

            if (currentUser.DeveloperMode != true)
                return Forbid("You do not have permission to perform this action");

            if (currentUser.Id == userId)
                return BadRequest("You cannot delete yourself");

            var userToDelete = await _context.Users.FindAsync(userId);
            if (userToDelete == null)
                return NotFound("User not found");

            try
            {
                _context.Users.Remove(userToDelete);
                await _context.SaveChangesAsync();
                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error deleting user {userId}: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        [HttpDelete("deletemyaccount")]
        public async Task<IActionResult> DeleteMyAccount([FromBody] DeleteAccountRequest request)
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return Unauthorized();

            // Verify password for security
            if (string.IsNullOrEmpty(request.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return BadRequest("Invalid password confirmation");

            // Optional: Add confirmation text check
            if (!string.Equals(request.ConfirmationText, "DELETE MY ACCOUNT", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Please type 'DELETE MY ACCOUNT' to confirm");

            try
            {
                // Delete user's CV file if it exists
                if (!string.IsNullOrEmpty(user.CVPath) && System.IO.File.Exists(user.CVPath))
                {
                    System.IO.File.Delete(user.CVPath);
                }

                // Remove user from database (cascade delete should handle related records)
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error deleting user account {user.Id}: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting your account. Please try again.");
            }
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