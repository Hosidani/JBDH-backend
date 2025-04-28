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

        public SettingsController(IUserPropertiesService userService)
        {
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
        // Update Organisation
        [HttpPost]
        public async Task<IActionResult> UpdateOrganisation(string newOrganisation)
        {
            try
            {
                var success = await _userPropertiesService.UpdateOrganisationAsync(newOrganisation);
                if (success)
                {
                    ViewData["SuccessMessage"] = "Your organisation has been successfully updated.";
                    return RedirectToAction("SuccessPage");
                }
                else
                {
                    ViewData["ErrorMessage"] = "Failed to update organisation.";
                    return View("ErrorPage");
                }
            }
            catch (Exception ex)
            {
                ViewData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return View("ErrorPage");
            }
        }

        // Update OrganisationId
        [HttpPost]
        public async Task<IActionResult> UpdateOrganisationId(int newOrganisationId)
        {
            try
            {
                var success = await _userPropertiesService.UpdateOrganisationIdAsync(newOrganisationId);
                if (success)
                {
                    ViewData["SuccessMessage"] = "Your organisation ID has been successfully updated.";
                    return RedirectToAction("SuccessPage");
                }
                else
                {
                    ViewData["ErrorMessage"] = "Failed to update organisation ID.";
                    return View("ErrorPage");
                }
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
                ViewData["OpenForHiring"] = user.openForHiring ?? false;
            }
            else
            {
                ViewData["OpenForHiring"] = false;
            }
            return View();
        }
    }
}