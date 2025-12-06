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

                // Map all fields from DTO to entity
                user.PasswordHash = dto.PasswordHash ?? user.PasswordHash;
                user.Name = dto.Name;
                user.Email = dto.Email ?? user.Email;
                user.Location = dto.Location ?? user.Location;
                user.DesiredEmploymentType = dto.DesiredEmploymentType;
                user.DesiredWorkMode = dto.DesiredWorkMode;
                user.PhoneNumber = dto.PhoneNumber;
                user.TravelAvailability = dto.TravelAvailability;
                user.AvailableFrom = dto.AvailableFrom;
                user.DesiredCity = dto.DesiredCity;
                user.WorkPlace = dto.WorkPlace;
                user.Competencies = dto.Competencies;
                user.CVPath = dto.CVPath;
                user.DeveloperMode = dto.DeveloperMode;
                user.Searchable = dto.Searchable;
                user.IsBanned = dto.IsBanned;
                user.AboutMe = dto.AboutMe;
                // SelectedRoles, SentFriendRequests, ReceivedFriendRequests, JobApplications, DataFlows, AIResponses, Roles, ManagedOrganisations, OwnedUsers, Owner, OwnerId are not mapped here (handled elsewhere or not updatable directly)

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Return updated DTO
                var aiResponseDtos = user.AIResponses?.Select(r => new UserAIResponseDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    ResponseText = r.ResponseText,
                    CreatedAt = r.CreatedAt,
                    Analysis = r.AnalysisResult
                }).ToList();

                var updatedDto = new UserDto
                {
                    Id = user.Id,
                    PasswordHash = user.PasswordHash,
                    Email = user.Email,
                    Location = user.Location,
                    Name = user.Name,
                    DesiredEmploymentType = user.DesiredEmploymentType,
                    DesiredWorkMode = user.DesiredWorkMode,
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