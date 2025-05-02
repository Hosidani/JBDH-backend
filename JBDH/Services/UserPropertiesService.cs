using JBDH.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JBDH.Services
{
    public class UserPropertiesService : IUserPropertiesService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;

        public UserPropertiesService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return null;

            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> UpdateEmailAsync(string newEmail)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                LoggingConfigService.ReturnInfo("User not found.");
                return false;
            }

            LoggingConfigService.ReturnInfo($"Received new email: {newEmail}");

            if (string.IsNullOrEmpty(newEmail))
            {
                LoggingConfigService.ReturnInfo("New email cannot be null or empty.");
                return false;
            }

            user.Email = newEmail;

            try
            {
                _context.Users.Update(user);  // Ensure that the update operation is explicitly called
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnInfo($"Error updating email: {ex.Message}");
                return false;
            }
        }
        // Update Organisation
        public async Task<bool> UpdateOrganisationAsync(string newOrganisation)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                LoggingConfigService.ReturnInfo("User not found.");
                return false;
            }

            user.Organisation = newOrganisation;
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnInfo($"Error updating organisation: {ex.Message}");
                return false;
            }
        }

        // Update OrganisationId
        public async Task<bool> UpdateOrganisationIdAsync(int newOrganisationId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                LoggingConfigService.ReturnInfo("User not found.");
                return false;
            }

            user.OrganisationId = newOrganisationId;
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnInfo($"Error updating organisation ID: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateOpenForHiringAsync(bool openForHiring)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            user.openForHiring = openForHiring;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDeveloperModeAsync(bool developerMode)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            user.developerMode = developerMode;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UploadCVAsync(IFormFile cvFile)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            if (cvFile == null || cvFile.Length == 0)
                return false;

            var extension = Path.GetExtension(cvFile.FileName).ToLowerInvariant();
            if (new[] { ".exe", ".js", ".bat" }.Contains(extension))
                return false;

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var newCvPath = Path.Combine(uploadsPath, cvFile.FileName);

            using (var stream = new FileStream(newCvPath, FileMode.Create))
            {
                await cvFile.CopyToAsync(stream);
            }

            // Delete old CV if exists
            if (!string.IsNullOrEmpty(user.CVPath))
            {
                var oldCvFullPath = Path.Combine(_environment.WebRootPath, user.CVPath);
                if (File.Exists(oldCvFullPath))
                    File.Delete(oldCvFullPath);
            }

            user.CVPath = Path.Combine("uploads", cvFile.FileName);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
