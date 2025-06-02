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
        private readonly IAIResponseService _aiResponseService;

        public SettingsController(IUserPropertiesService userService, ApplicationDbContext context, IWebHostEnvironment environment, IAIService aiService, IAIResponseService aiResponseService)
        {
            _context = context;
            _environment = environment;
            _aiService = aiService;
            _userPropertiesService = userService;
            _aiResponseService = aiResponseService;
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