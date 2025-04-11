using JBDH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using ApplicationDbContext = JBDH.Models.ApplicationDbContext;

namespace JBDH.Controllers
{
    //[Authorize] // Ensure only logged-in users can access this controller
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
            LoggingConfig.ReturnInfo("ChangeCV method called");
            // Check if a file was uploaded
            if (cvFile == null || cvFile.Length == 0)
            {
                LoggingConfig.ReturnError("No file selected for upload.");
                ViewData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("Settings", "Home");
            }
            // Optional: Add a file size limit (e.g., 5 MB)
            if (cvFile.Length > 5 * 1024 * 1024) // 5 MB limit
            {
                LoggingConfig.ReturnError("File size exceeds the 5 MB limit.");
                ViewData["ErrorMessage"] = "File size exceeds the 5 MB limit.";
                return RedirectToAction("Settings", "Home");
            }
            // Check for potentially unsafe file extensions
            var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (new[] { ".exe", ".js", ".bat" }.Contains(extension))
            {
                LoggingConfig.ReturnWarning($"Potentially unsafe file uploaded: {cvFile.FileName}");
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
                    LoggingConfig.ReturnError("User not found.");
                    ViewData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Settings", "Home");
                }
                // Delete the old CV file if it exists
                if (!string.IsNullOrEmpty(user.CVPath))
                {
                    LoggingConfig.ReturnInfo($"Deleting old CV file: {user.CVPath}");
                    var oldCvPath = Path.Combine(_environment.WebRootPath, user.CVPath);
                    if (System.IO.File.Exists(oldCvPath))
                    {
                        LoggingConfig.ReturnInfo($"Old CV file found and deleted: {oldCvPath}");
                        System.IO.File.Delete(oldCvPath); // Delete the old CV file
                    }
                }
                // Save the new CV file
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    LoggingConfig.ReturnInfo($"Creating uploads directory: {uploadsPath}");
                    Directory.CreateDirectory(uploadsPath);
                }
                var newCvPath = Path.Combine(uploadsPath, cvFile.FileName);
                using (var stream = new FileStream(newCvPath, FileMode.Create))
                {
                    LoggingConfig.ReturnInfo($"Saving new CV file: {newCvPath}");
                    cvFile.CopyTo(stream);
                }
                // Update the user's CV path in the database
                user.CVPath = Path.Combine("uploads", cvFile.FileName);
                _context.SaveChanges();
                LoggingConfig.ReturnInfo($"CV updated for user: {username}");
                ViewData["SuccessMessage"] = "CV updated successfully!";
            }
            catch (Exception ex)
            {
                LoggingConfig.ReturnError($"Error updating CV: {ex.Message}");
                ViewData["ErrorMessage"] = $"An error occurred while updating your CV: {ex.Message}";
            }
            return RedirectToAction("Settings", "Home");
        }
        
        [HttpGet]
        public IActionResult Settings()
        {
            LoggingConfig.ReturnInfo("Settings action called");
            return View();
        }

        [HttpGet]
        public IActionResult Test()
        {
            return Content("Settings route is working!");
        }
    }
}