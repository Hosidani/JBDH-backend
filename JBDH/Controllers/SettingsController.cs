using JBDH.Models.Core;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using JBDH.Models.ViewModels;
using JBDH.Helpers;

namespace JBDH.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> ChangeCV(IFormFile cvFile)
        {
            LoggingConfigService.ReturnInfo("ChangeCV method called with file: " + (cvFile?.FileName ?? "No file uploaded"));
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return this.ErrorPage("User not found.");

            if (cvFile == null)
                return this.ErrorPage("No file uploaded.");

            var result = await _userPropertiesService.ChangeCVAndAnalyzeAsync(cvFile, user.Id);
            if (result.Success)
                return this.SetSuccessAndRedirect("CV updated successfully!", "Settings", new { controller = "Home" });
            else
                return this.SetErrorAndRedirect(result.ErrorMessage ?? "Failed to update CV.", "Settings", new { controller = "Home" });
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleOpenForHire(bool openForHiring)
        {
            var success = await _userPropertiesService.UpdateOpenForHiringAsync(openForHiring);
            if (success)
                return this.SetSuccessAndRedirect("Open for hiring status updated successfully!", "Settings", new { controller = "Home" });
            else
                return this.SetErrorAndRedirect("Failed to update status.", "Settings", new { controller = "Home" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string newEmail)
        {
            try
            {
                var success = await _userPropertiesService.UpdateEmailAsync(newEmail);
                if (success)
                    return this.SetSuccessAndRedirect("Email updated successfully!", "SuccessPage");
                else
                    return this.ErrorPage("Email update failed.");
            }
            catch (ArgumentException ex)
            {
                return this.ErrorPage(ex.Message);
            }
            catch (Exception ex)
            {
                return this.ErrorPage("An error occurred: " + ex.Message);
            }
        }

        public IActionResult SuccessPage()
        {
            return View();  // Return the SuccessPage view
        }

        [HttpGet]
        public IActionResult Settings()
        {
        var model = new ManageRolesViewModel
        {
            AvailableRoles = new List<string> { "Manager", "Recruiter" },
            SelectedRoles = new List<string>()
        };
        return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles()
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Settings");

            var model = new ManageRolesViewModel
            {
                UserId = user.Id,
                // Only show roles that can be changed (not "User")
                SelectedRoles = user.Roles
                    .Where(r => r.Role != "User")
                    .Select(r => r.Role)
                    .ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageRoles(ManageRolesViewModel model)
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out var userId))
                return this.ErrorPage("Invalid user ID.");

            var success = await _userRoleService.UpdateUserRolesAsync(userId, model.SelectedRoles);
            if (success)
                return this.SetSuccessAndRedirect("Roles updated successfully!", "Settings");
            else
                return this.SetErrorAndRedirect("Failed to update roles.", "Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser()
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
                return this.ErrorPage("User not found.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await HttpContext.SignOutAsync("CookieAuth");
            return this.SetSuccessAndRedirect("Your account has been deleted.", "Index", new { controller = "Home" });
        }

        private async Task<bool> DeleteUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}