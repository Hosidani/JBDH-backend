using JBDH.Models.Core;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using Microsoft.AspNetCore.Authentication;
using JBDH.Models.ViewModels;
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
                IAIApiClient aIApiClient,
                IUserRoleService userRoleService
            )
            {
                _context = context;
                _environment = environment;
                _aIApiClient = aIApiClient;
                _userPropertiesService = userService;
                _userRoleService = userRoleService;
            }

            [HttpPost]
            public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
            {
                var user = await _userPropertiesService.GetCurrentUserAsync();
                if (user == null)
                    return this.ErrorPage("User not found.");

                // CV
                if (request.CvFile != null)
                {
                    var result = await _userPropertiesService.ChangeCVAndAnalyzeAsync(request.CvFile, user.Id);
                    if (!result.Success)
                        return this.SetErrorAndRedirect(result.ErrorMessage ?? "Failed to update CV.", "Settings", new { controller = "Home" });
                }

                // Email
                if (!string.IsNullOrWhiteSpace(request.NewEmail))
                {
                    var success = await _userPropertiesService.UpdateEmailAsync(request.NewEmail);
                    if (!success)
                        return this.ErrorPage("Email update failed.");
                }

                // Searchable hanteras nedan i user-modifieringslogik

                // Roles
                if (request.SelectedRoles != null)
                {
                    var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(userIdString, out var userId))
                        return this.ErrorPage("Invalid user ID.");
                    var success = await _userRoleService.UpdateUserRolesAsync(userId, request.SelectedRoles);
                    if (!success)
                        return this.SetErrorAndRedirect("Failed to update roles.", "Settings");
                }

                // Modifiera alla fält i User
                bool userModified = false;
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    user.Name = request.Name;
                    userModified = true;
                }
                if (!string.IsNullOrWhiteSpace(request.DesiredEmploymentType))
                {
                    user.DesiredEmploymentType = request.DesiredEmploymentType;
                    userModified = true;
                }
                if (!string.IsNullOrWhiteSpace(request.DesiredWorkMode))
                {
                    user.DesiredWorkMode = request.DesiredWorkMode;
                    userModified = true;
                }
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    user.PhoneNumber = request.PhoneNumber;
                    userModified = true;
                }
                if (request.TravelAvailability.HasValue)
                {
                    user.TravelAvailability = request.TravelAvailability.Value;
                    userModified = true;
                }
                if (request.AvailableFrom.HasValue)
                {
                    user.AvailableFrom = request.AvailableFrom.Value;
                    userModified = true;
                }
                if (!string.IsNullOrWhiteSpace(request.DesiredCity))
                {
                    user.DesiredCity = request.DesiredCity;
                    userModified = true;
                }
                if (!string.IsNullOrWhiteSpace(request.Competencies))
                {
                    user.Competencies = request.Competencies;
                    userModified = true;
                }
                if (!string.IsNullOrWhiteSpace(request.Location))
                {
                    user.Location = request.Location;
                    userModified = true;
                }
                if (!string.IsNullOrWhiteSpace(request.AboutMe))
                {
                    user.AboutMe = request.AboutMe;
                    userModified = true;
                }
                if (request.Searchable.HasValue)
                {
                    user.Searchable = request.Searchable.Value;
                    userModified = true;
                }
                if (request.DeveloperMode.HasValue)
                {
                    user.DeveloperMode = request.DeveloperMode.Value;
                    userModified = true;
                }
                if (!string.IsNullOrWhiteSpace(request.WorkPlace))
                {
                    user.WorkPlace = request.WorkPlace;
                    userModified = true;
                }
                // Email
                if (!string.IsNullOrWhiteSpace(request.NewEmail))
                {
                    var success = await _userPropertiesService.UpdateEmailAsync(request.NewEmail);
                    if (!success)
                        return this.ErrorPage("Email update failed.");
                }
                // Password
                if (!string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    // Hasha lösenordet (exempel med SHA256, byt till bättre om du har Identity)
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(request.NewPassword);
                        var hash = sha.ComputeHash(bytes);
                        user.PasswordHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
                        userModified = true;
                    }
                }
                if (userModified)
                {
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }

                // Delete user
                if (request.DeleteUser.HasValue && request.DeleteUser.Value)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    await HttpContext.SignOutAsync("CookieAuth");
                    return this.SetSuccessAndRedirect("Your account has been deleted.", "Index", new { controller = "Home" });
                }

                return this.SetSuccessAndRedirect("Profile updated successfully!", "Settings", new { controller = "Home" });
            }

            public class UpdateProfileRequest
            {
                public IFormFile? CvFile { get; set; }
                public string? Name { get; set; }
                public string? NewEmail { get; set; }
                public string? NewPassword { get; set; }
                // public bool? OpenForHiring { get; set; } // tas bort, ersätts av Searchable
                public List<string>? SelectedRoles { get; set; }
                public bool? DeleteUser { get; set; }
                public string? DesiredEmploymentType { get; set; }
                public string? DesiredWorkMode { get; set; }
                public string? PhoneNumber { get; set; }
                public bool? TravelAvailability { get; set; }
                public DateTime? AvailableFrom { get; set; }
                public string? DesiredCity { get; set; }
                public string? Competencies { get; set; }
                public string? Location { get; set; }
                public string? AboutMe { get; set; }
                public bool? Searchable { get; set; }
                public bool? DeveloperMode { get; set; }
                public string? WorkPlace { get; set; }
            }
        }
}