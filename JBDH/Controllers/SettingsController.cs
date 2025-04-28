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
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost]
        public IActionResult ChangeCV(IFormFile cvFile)
        {
            LoggingConfigService.ReturnInfo("ChangeCV method called");
            // Check if a file was uploaded
            if (cvFile == null || cvFile.Length == 0)
            {
                LoggingConfigService.ReturnError("No file selected for upload.");
                ViewData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("Settings", "Home");
            }
            // Optional: Add a file size limit (e.g., 5 MB)
            if (cvFile.Length > 5 * 1024 * 1024) // 5 MB limit
            {
                LoggingConfigService.ReturnError("File size exceeds the 5 MB limit.");
                ViewData["ErrorMessage"] = "File size exceeds the 5 MB limit.";
                return RedirectToAction("Settings", "Home");
            }
            // Check for potentially unsafe file extensions
            var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (new[] { ".exe", ".js", ".bat" }.Contains(extension))
            {
                LoggingConfigService.ReturnWarning($"Potentially unsafe file uploaded: {cvFile.FileName}");
                ViewData["ErrorMessage"] = "The selected file type is not allowed.";
                return RedirectToAction("Settings", "Home");
            }
            try
            {
                // Get the logged-in user's username
                var username = User.Identity.Name;
                var user = _context.Users.FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    LoggingConfigService.ReturnError("User not found.");
                    ViewData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Settings", "Home");
                }
                // Delete the old CV file if it exists
                if (!string.IsNullOrEmpty(user.CVPath))
                {
                    LoggingConfigService.ReturnInfo($"Deleting old CV file: {user.CVPath}");
                    var oldCvPath = Path.Combine(_environment.WebRootPath, user.CVPath);
                    if (System.IO.File.Exists(oldCvPath))
                    {
                        LoggingConfigService.ReturnInfo($"Old CV file found and deleted: {oldCvPath}");
                        System.IO.File.Delete(oldCvPath); // Delete the old CV file
                    }
                }
                // Save the new CV file
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    LoggingConfigService.ReturnInfo($"Creating uploads directory: {uploadsPath}");
                    Directory.CreateDirectory(uploadsPath);
                }
                var newCvPath = Path.Combine(uploadsPath, cvFile.FileName);
                using (var stream = new FileStream(newCvPath, FileMode.Create))
                {
                    LoggingConfigService.ReturnInfo($"Saving new CV file: {newCvPath}");
                    cvFile.CopyTo(stream);
                }
                // Update the user's CV path in the database
                user.CVPath = Path.Combine("uploads", cvFile.FileName);
                _context.SaveChanges();
                LoggingConfigService.ReturnInfo($"CV updated for user: {username}");
                ViewData["SuccessMessage"] = "CV updated successfully!";
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error updating CV: {ex.Message}");
                ViewData["ErrorMessage"] = $"An error occurred while updating your CV: {ex.Message}";
            }
            return RedirectToAction("Settings", "Home");
        }
        
        [HttpPost]
        public IActionResult ToggleOpenForHire(bool openForHiring)
        {
            // Get the logged-in user's username
            LoggingConfigService.ReturnInfo($"Current user in ToggleOpenForHire: {User.Identity.Name}");
            LoggingConfigService.ReturnInfo($"openForHiring received from form: {openForHiring}");
            var username = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                //LoggingConfigService.ReturnError("User not found.");
                ViewData["ErrorMessage"] = "User not found.";
                return View("Settings");
            }
            LoggingConfigService.ReturnInfo($"Before update, openForHiring = {user.openForHiring}");
            // Update the user's openForHiring status in the database
            user.openForHiring = openForHiring;
            _context.SaveChanges();
            LoggingConfigService.ReturnInfo($"After update, openForHiring = {user.openForHiring}");
            LoggingConfigService.ReturnInfo($"Open for hiring status updated for user: {username}");
            ViewData["SuccessMessage"] = "Open for hiring status updated successfully!";
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult Settings()
        {
            // Check if the user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                // Retrieve the username from the claims
                var username = User.Identity.Name;
                LoggingConfigService.ReturnInfo($"Current user in Settings: {username}");
                var user = _context.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    ViewData["OpenForHiring"] = user.openForHiring ?? false;
                }
            }
            else
            {
                // If not authenticated, print a message
                LoggingConfigService.ReturnError("User is not authenticated.");
                ViewData["OpenForHiring"] = false;
            }
            return View();  // Render the settings view
        }
    }
}