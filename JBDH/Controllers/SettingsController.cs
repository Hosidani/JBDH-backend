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
using JBDH.Models.User;
using JBDH.Models.Organisation;
using Microsoft.EntityFrameworkCore;

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

            [HttpGet("getaccountsettings")]
            public async Task<IActionResult> GetAccountSettings()
            {
                var user = await _userPropertiesService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                var account = await _context.Accounts
                    .Include(a => a.User)
                        .ThenInclude(u => u.Roles)
                    .FirstOrDefaultAsync(a => a.UserId == user.Id);

                if (account == null)
                    return NotFound(new { message = "No account found for user." });

                if (account.IsTerminated)
                    return Unauthorized(new { message = "Account has been terminated." });

                var roles = account.User.Roles?.Select(r => r.Role).ToList() ?? new List<string>();

                // Get organisations where user is manager OR member
                var organisations = new List<OrganisationDto>();
                
                // Find organisations where user is manager (in Managers collection)
                var managedOrgs = await _context.Organisations
                    .Where(org => org.Managers.Any(m => m.Id == user.Id))
                    .ToListAsync();
                
                organisations.AddRange(managedOrgs.Select(org => new OrganisationDto
                {
                    Id = org.Id,
                    Name = org.Name,
                    Role = "Manager"
                }));

                // Find organisations where user is member (client evaluation needed for JSON)
                var allOrgs = await _context.Organisations.ToListAsync();
                var memberOrgs = allOrgs.Where(org => org.Users.Contains(user.Id) && 
                                                      !managedOrgs.Any(m => m.Id == org.Id)).ToList();
                
                organisations.AddRange(memberOrgs.Select(org => new OrganisationDto
                {
                    Id = org.Id,
                    Name = org.Name,
                    Role = "Member"
                }));

                var accountDto = new AccountDto
                {
                    Id = account.Id,
                    UserId = account.UserId,
                    AccountName = account.AccountName,
                    AccountEmail = account.AccountEmail,
                    AccountPhone = account.AccountPhone,
                    PreferredLanguage = account.PreferredLanguage,
                    AccountType = account.AccountType,
                    IsTerminated = account.IsTerminated,
                    TerminatedAt = account.TerminatedAt,
                    AcceptedTermsAndConditions = account.AcceptedTermsAndConditions,
                    TermsAcceptedAt = account.TermsAcceptedAt,
                    CreatedAt = account.CreatedAt,
                    UpdatedAt = account.UpdatedAt,
                    Roles = roles,
                    Organisations = organisations
                };

                return Ok(accountDto);
            }

            [HttpPut("updateaccountsettings")]
            public async Task<IActionResult> UpdateAccountSettings([FromBody] AccountDto dto)
            {
                var user = await _userPropertiesService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                var account = await _context.Accounts
                    .Include(a => a.User)
                        .ThenInclude(u => u.Roles)
                    .Include(a => a.User)
                        .ThenInclude(u => u.ManagedOrganisations)
                    .FirstOrDefaultAsync(a => a.UserId == user.Id);

                if (account == null)
                {
                    // Create a new account if none exists
                    account = new Account
                    {
                        UserId = user.Id,
                        AccountName = dto.AccountName ?? user.Name ?? user.Email,
                        AccountEmail = dto.AccountEmail ?? user.Email,
                        AccountPhone = dto.AccountPhone,
                        PreferredLanguage = dto.PreferredLanguage,
                        AccountType = dto.AccountType,
                        AcceptedTermsAndConditions = dto.AcceptedTermsAndConditions,
                        TermsAcceptedAt = dto.AcceptedTermsAndConditions ? DateTime.UtcNow : null
                    };
                    _context.Accounts.Add(account);
                }
                else
                {
                    // Check if account is terminated
                    if (account.IsTerminated)
                        return Unauthorized(new { message = "Cannot update terminated account." });

                    // Update existing account basic fields
                    if (!string.IsNullOrWhiteSpace(dto.AccountName))
                        account.AccountName = dto.AccountName;
                    if (!string.IsNullOrWhiteSpace(dto.AccountEmail))
                        account.AccountEmail = dto.AccountEmail;
                    if (!string.IsNullOrWhiteSpace(dto.AccountPhone))
                        account.AccountPhone = dto.AccountPhone;
                    if (!string.IsNullOrWhiteSpace(dto.PreferredLanguage))
                        account.PreferredLanguage = dto.PreferredLanguage;
                    
                    account.AccountType = dto.AccountType;
                    
                    // Handle terms acceptance
                    if (dto.AcceptedTermsAndConditions && !account.AcceptedTermsAndConditions)
                    {
                        account.AcceptedTermsAndConditions = true;
                        account.TermsAcceptedAt = DateTime.UtcNow;
                    }
                    
                    account.UpdatedAt = DateTime.UtcNow;
                }

                // Handle role updates
                if (dto.UpdateRoles != null)
                {
                    var validRoles = new[] { "User", "Manager", "Recruiter", "Sales" };
                    var invalidRoles = dto.UpdateRoles.Where(r => !validRoles.Contains(r)).ToList();
                    if (invalidRoles.Any())
                        return BadRequest(new { message = $"Invalid roles: {string.Join(", ", invalidRoles)}" });

                    // Remove existing roles
                    var existingRoles = _context.UserRoleAssignments.Where(r => r.UserId == user.Id);
                    _context.UserRoleAssignments.RemoveRange(existingRoles);

                    // Add new roles
                    foreach (var role in dto.UpdateRoles.Distinct())
                    {
                        _context.UserRoleAssignments.Add(new UserRoleAssignment
                        {
                            UserId = user.Id,
                            User = user,
                            Role = role
                        });
                    }
                }

                // Handle organisation joins
                if (dto.JoinOrganisationIds != null)
                {
                    foreach (var orgId in dto.JoinOrganisationIds.Distinct())
                    {
                        var organisation = await _context.Organisations.FindAsync(orgId);
                        if (organisation != null && !organisation.Users.Contains(user.Id))
                        {
                            organisation.Users.Add(user.Id);
                        }
                    }
                }

                // Handle organisation leaves
                if (dto.LeaveOrganisationIds != null)
                {
                    foreach (var orgId in dto.LeaveOrganisationIds.Distinct())
                    {
                        var organisation = await _context.Organisations.FindAsync(orgId);
                        if (organisation != null)
                        {
                            // Remove from members
                            if (organisation.Users.Contains(user.Id))
                                organisation.Users.Remove(user.Id);
                            
                            // Remove from managers if applicable
                            var managedOrg = user.ManagedOrganisations?.FirstOrDefault(o => o.Id == orgId);
                            if (managedOrg != null)
                                user.ManagedOrganisations?.Remove(managedOrg);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Reload data to get updated relationships
                account = await _context.Accounts
                    .Include(a => a.User)
                        .ThenInclude(u => u.Roles)
                    .Include(a => a.User)
                        .ThenInclude(u => u.ManagedOrganisations)
                    .FirstOrDefaultAsync(a => a.UserId == user.Id);

                // Get updated roles
                var roles = account!.User.Roles?.Select(r => r.Role).ToList() ?? new List<string>();

                // Get updated organisations
                var organisations = new List<OrganisationDto>();
                if (account.User.ManagedOrganisations?.Any() == true)
                {
                    organisations.AddRange(account.User.ManagedOrganisations.Select(org => new OrganisationDto
                    {
                        Id = org.Id,
                        Name = org.Name,
                        Role = "Manager"
                    }));
                }

                var updatedDto = new AccountDto
                {
                    Id = account.Id,
                    UserId = account.UserId,
                    AccountName = account.AccountName,
                    AccountEmail = account.AccountEmail,
                    AccountPhone = account.AccountPhone,
                    PreferredLanguage = account.PreferredLanguage,
                    AccountType = account.AccountType,
                    IsTerminated = account.IsTerminated,
                    TerminatedAt = account.TerminatedAt,
                    AcceptedTermsAndConditions = account.AcceptedTermsAndConditions,
                    TermsAcceptedAt = account.TermsAcceptedAt,
                    CreatedAt = account.CreatedAt,
                    UpdatedAt = account.UpdatedAt,
                    Roles = roles,
                    Organisations = organisations
                };

                return Ok(updatedDto);
            }

            [HttpPost("terminateaccount")]
            public async Task<IActionResult> TerminateAccount()
            {
                var user = await _userPropertiesService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.UserId == user.Id && !a.IsTerminated);

                if (account == null)
                    return NotFound(new { message = "No active account found for user." });

                // Terminate the account (soft delete)
                account.IsTerminated = true;
                account.TerminatedAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Account terminated" });
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

            [HttpGet("getavailableorganisations")]
            public async Task<IActionResult> GetAvailableOrganisations()
            {
                var user = await _userPropertiesService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized();

                // Get user's current organisation IDs (both managed and member)
                var currentOrgIds = new List<int>();
                
                var userWithOrgs = await _context.Users
                    .Include(u => u.ManagedOrganisations)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);
                
                if (userWithOrgs?.ManagedOrganisations != null)
                    currentOrgIds.AddRange(userWithOrgs.ManagedOrganisations.Select(o => o.Id));

                // TODO: Add member organisations when Owner/OwnerId relationship is implemented
                
                // Return organisations user is NOT already member of
                var availableOrgs = await _context.Organisations
                    .Where(org => !currentOrgIds.Contains(org.Id))
                    .Select(org => new OrganisationDto
                    {
                        Id = org.Id,
                        Name = org.Name,
                        Role = "" // Tom för available orgs
                    })
                    .ToListAsync();

                return Ok(availableOrgs);
            }
        }
    }