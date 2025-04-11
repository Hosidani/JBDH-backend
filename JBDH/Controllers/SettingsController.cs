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
            if (cvFile == null || cvFile.Length == 0)
            {
                LoggingConfig.ReturnError("No file selected for upload.");
                ViewData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("Settings", "Settings"); // Fixed
            }

            if (cvFile.Length > 5 * 1024 * 1024) // 5 MB limit
            {
                LoggingConfig.ReturnError("File size exceeds the 5 MB limit.");
                ViewData["ErrorMessage"] = "File size exceeds the 5 MB limit.";
                return RedirectToAction("Settings", "Settings"); // Fixed
            }

            var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (new[] { ".exe", ".js", ".bat" }.Contains(extension))
            {
                LoggingConfig.ReturnWarning($"Potentially unsafe file uploaded: {cvFile.FileName}");
                ViewData["ErrorMessage"] = "The selected file type is not allowed.";
                return RedirectToAction("Settings", "Settings"); // Fixed
            }

            try
            {
                var username = User.Identity.Name;
                var user = _context.Users.FirstOrDefault(u => u.Username == username);
                if (user == null)
                {
                    LoggingConfig.ReturnError("User not found.");
                    ViewData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Settings", "Settings"); // Fixed
                }

                if (!string.IsNullOrEmpty(user.CVPath))
                {
                    var oldCvPath = Path.Combine(_environment.WebRootPath, user.CVPath);
                    if (System.IO.File.Exists(oldCvPath))
                    {
                        System.IO.File.Delete(oldCvPath);
                    }
                }

                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var newCvPath = Path.Combine(uploadsPath, cvFile.FileName);
                using (var stream = new FileStream(newCvPath, FileMode.Create))
                {
                    cvFile.CopyTo(stream);
                }

                user.CVPath = Path.Combine("uploads", cvFile.FileName);
                _context.SaveChanges();
                ViewData["SuccessMessage"] = "CV updated successfully!";
            }
            catch (Exception ex)
            {
                LoggingConfig.ReturnError($"Error updating CV: {ex.Message}");
                ViewData["ErrorMessage"] = $"An error occurred while updating your CV: {ex.Message}";
            }

            return RedirectToAction("Settings", "Settings"); // Fixed
        }
        
        [HttpGet]
        public IActionResult Settings()
        {
            LoggingConfig.ReturnInfo("Settings action called");
            return View();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Settings", "Settings"); // Redirect to the Settings action
        }
    }
}