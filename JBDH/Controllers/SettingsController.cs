using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace JBDH.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly IUserPropertiesService _userPropertiesService; 
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        
        private readonly IAIService _aiService;

        public SettingsController(IUserPropertiesService userService, ApplicationDbContext context, IWebHostEnvironment environment, IAIService aiService)
        {
            _context = context;
            _environment = environment;
            _aiService = aiService;
            _userPropertiesService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> ChangeCV(IFormFile cvFile)
        {
            var success = await _userPropertiesService.UploadCVAsync(cvFile);
            if (success)
                ViewData["SuccessMessage"] = "CV updated successfully!";
            else
                ViewData["ErrorMessage"] = "Failed to upload CV.";
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user == null)
            {
                ViewData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Settings", "Home");
            }
            var aiResponse = await _aiService.GetAIResponse(user, 0);
            var viewModel = new ProfileViewModel
            {
                User = user,
                IsFriend = false, // Doesn't matter here, since it's the user's own profile
                AIResponse = aiResponse
            };
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
        public async Task<IActionResult> Settings()
        {
            var user = await _userPropertiesService.GetCurrentUserAsync();
            if (user != null)
            {
                ViewData["OpenForHiring"] = user.OpenForHiring ?? false; // Updated property casing
            }
            else
            {
                ViewData["OpenForHiring"] = false;
            }
            return View();
        }
    }
}