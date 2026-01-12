using JBDH.Models.Core;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using Microsoft.AspNetCore.Authentication;
using JBDH.Models.DTOs;
using JBDH.Helpers;

namespace JBDH.Controllers
{
        [Route("api/Settings")]
        [ApiController]
        [Authorize]
        [IncludeInApi]
        public class SettingsController : Controller
        {
            private readonly IUserPropertiesService _userPropertiesService;
            private readonly ApplicationDbContext _context;
            private readonly IWebHostEnvironment _environment;
            private readonly IAIApiClient _aIApiClient;
            private readonly IUserRoleService _userRoleService;

            public SettingsController(
                IUserPropertiesService userService,
                ApplicationDbContext context,
                IWebHostEnvironment environment,
                IAIApiClient aIAApiClient,
                IUserRoleService userRoleService
            )
            {
                _context = context;
                _environment = environment;
                _aIApiClient = aIAApiClient;
                _userPropertiesService = userService;
                _userRoleService = userRoleService;
            }

            [HttpPut("updateusersettings")]
            public async Task<IActionResult> UpdateUserSettings([FromBody] UserDto dto)
            {
                var user = await _userPropertiesService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                // Only update fields that are present and valid in the DTO
                if (!string.IsNullOrWhiteSpace(dto.Name))
                    user.Name = dto.Name;
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
                    user.Email = dto.Email;
                if (!string.IsNullOrWhiteSpace(dto.Location))
                    user.Location = dto.Location;
                if (dto.DesiredEmploymentTypes != null && dto.DesiredEmploymentTypes.Any())
                    user.DesiredEmploymentTypes = dto.DesiredEmploymentTypes;
                if (dto.DesiredWorkModes != null && dto.DesiredWorkModes.Any())
                    user.DesiredWorkModes = dto.DesiredWorkModes;
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                    user.PhoneNumber = dto.PhoneNumber;
                if (dto.TravelAvailability.HasValue)
                    user.TravelAvailability = dto.TravelAvailability.Value;
                if (dto.AvailableFrom.HasValue)
                    user.AvailableFrom = dto.AvailableFrom;
                if (!string.IsNullOrWhiteSpace(dto.DesiredCity))
                    user.DesiredCity = dto.DesiredCity;
                if (!string.IsNullOrWhiteSpace(dto.WorkPlace))
                    user.WorkPlace = dto.WorkPlace;
                if (!string.IsNullOrWhiteSpace(dto.Competencies))
                    user.Competencies = dto.Competencies;
                if (!string.IsNullOrWhiteSpace(dto.CVPath))
                    user.CVPath = dto.CVPath;
                if (dto.DeveloperMode.HasValue)
                    user.DeveloperMode = dto.DeveloperMode;
                if (dto.Searchable.HasValue)
                    user.Searchable = dto.Searchable;
                user.IsBanned = dto.IsBanned;
                if (!string.IsNullOrWhiteSpace(dto.AboutMe))
                    user.AboutMe = dto.AboutMe;
                // SelectedRoles, SentFriendRequests, ReceivedFriendRequests, JobApplications, DataFlows, AIResponses, Roles, ManagedOrganisations, OwnedUsers, Owner, OwnerId are not mapped here (handled elsewhere or not updatable directly)

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Return updated DTO
                var aiResponseDtos = user.AIResponses?.Select(r => new UserAIResponseDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    ResponseText = r.ResponseText ?? string.Empty,
                    CreatedAt = r.CreatedAt,
                    Analysis = r.AnalysisResult
                }).ToList();

                var updatedDto = new UserDto
                {
                    Id = user.Id,
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
                return Ok(updatedDto);
            }

            [HttpPost("changepassword")]
            public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
            {
                var user = await _userPropertiesService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                    return BadRequest("Current and new password are required.");

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                    return BadRequest("Current password is incorrect.");

                // Optional: check new password strength
                if (dto.NewPassword.Length < 6)
                    return BadRequest("New password must be at least 6 characters.");

                if (!string.IsNullOrEmpty(dto.ConfirmNewPassword) && dto.NewPassword != dto.ConfirmNewPassword)
                    return BadRequest("New password and confirmation do not match.");

                // Hash and update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Password changed successfully." });
            }            
            
            [HttpDelete("deletemyuser")]
            public async Task<IActionResult> DeleteMyUser()
            {
                var user = await _userPropertiesService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await HttpContext.SignOutAsync("CookieAuth");
                return this.SetSuccessAndRedirect("Your account has been deleted.", "Index", new { controller = "Home" });
            }
        }
    }