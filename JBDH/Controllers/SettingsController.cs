using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using JBDH.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;

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
            {
                ViewData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Settings", "Home");
            }

            if (cvFile == null)
            {
                ViewData["ErrorMessage"] = "No file uploaded.";
                return RedirectToAction("Settings", "Home");
            }

            var result = await _userPropertiesService.ChangeCVAndAnalyzeAsync(cvFile, user.Id);
            if (result.Success)
                ViewData["SuccessMessage"] = "CV updated successfully!";
            else
                ViewData["ErrorMessage"] = result.ErrorMessage;

            return RedirectToAction("Settings", "Home");
        }
        
        [HttpPost]
        public async Task<IActionResult> ToggleOpenForHire(bool openForHiring)
        {
            var success = await _userPropertiesService.UpdateOpenForHiringAsync(openForHiring);
            if (success)
                ViewData["SuccessMessage"] = "Open for hiring status updated successfully!";
            else
                ViewData["ErrorMessage"] = "Failed to update status.";

            return RedirectToAction("Settings", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string newEmail)
        {
            try
            {
                var success = await _userPropertiesService.UpdateEmailAsync(newEmail);
                if (success)
                {
                    return RedirectToAction("SuccessPage");
                }
                else
                {
                    ViewData["ErrorMessage"] = "Email update failed.";
                    return View("ErrorPage");
                }
            }
            catch (ArgumentException ex)
            {
                ViewData["ErrorMessage"] = ex.Message;
                return View("ErrorPage");
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return View("ErrorPage");
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
                return Unauthorized();

            var success = await _userRoleService.UpdateUserRolesAsync(userId, model.SelectedRoles);
            if (success)
                TempData["SuccessMessage"] = "Roles updated successfully!";
            else
                TempData["ErrorMessage"] = "Failed to update roles.";

            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser()
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
            {
                ViewData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Settings");
            }

            // Remove user from database
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Sign out the user
            await HttpContext.SignOutAsync("CookieAuth");

            // Optionally, show a goodbye page or redirect to home/login
            TempData["SuccessMessage"] = "Your account has been deleted.";
            return RedirectToAction("Index", "Home");
        }
    }
}