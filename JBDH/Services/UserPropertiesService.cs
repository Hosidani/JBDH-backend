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
        private readonly IServiceProvider _serviceProvider;

        public UserPropertiesService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, IServiceProvider serviceProvider)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
            _serviceProvider = serviceProvider;
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

        // Removed UpdateOrganisationAsync and UpdateOrganisationIdAsync as they are no longer relevant

        public async Task<bool> UpdateOpenForHiringAsync(bool openForHiring)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            user.OpenForHiring = openForHiring; // Updated property casing
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDeveloperModeAsync(bool developerMode)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return false;

            user.DeveloperMode = developerMode; // Updated property casing
            await _context.SaveChangesAsync();
            return true;
        }

        private string GetAbsoluteCVPath(string cvFileName) // Helper method to get the absolute path of the CV file
        {
            if (string.IsNullOrEmpty(cvFileName)) return null;
            return Path.Combine(_environment.WebRootPath, "uploads", cvFileName);
        }

        public async Task<bool> UploadCVAsync(IFormFile cvFile) // Method to upload CV file
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
                var oldCvFullPath = GetAbsoluteCVPath(user.CVPath);
                if (!string.IsNullOrEmpty(oldCvFullPath) && File.Exists(oldCvFullPath))
                    File.Delete(oldCvFullPath);
            }

            user.CVPath = cvFile.FileName; // Store only filename
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string ErrorMessage)> ChangeCVAndAnalyzeAsync(IFormFile cvFile, int userId) // Method to change CV and analyze it
        {
            LoggingConfigService.ReturnInfo("ChangeCVAndAnalyzeAsync method called.");
            var user = await _context.Users.FindAsync(userId);
            LoggingConfigService.ReturnInfo($"User found: {user?.Username ?? "null"}");
            if (user == null)
            {
                LoggingConfigService.ReturnInfo("User not found.");
                return (false, "User not found.");
            }
            if (cvFile == null || cvFile.Length == 0)
            {
                LoggingConfigService.ReturnInfo("No CV file provided.");
                return (false, "No CV file provided.");
            }
            LoggingConfigService.ReturnInfo($"Received CV file: {cvFile.FileName}");
            var uploadSuccess = await UploadCVAsync(cvFile);
            if (!uploadSuccess)
            {
                LoggingConfigService.ReturnInfo("Failed to upload CV.");
                return (false, "Failed to upload CV.");
            }
            LoggingConfigService.ReturnInfo($"CV upload success: {uploadSuccess}");
            // Run AI analysis in the background
            Task.Run(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
                    var aiResponseService = scope.ServiceProvider.GetRequiredService<IAIResponseService>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    try
                    {
                        LoggingConfigService.ReturnInfo("Starting AI analysis for user CV.");
                        // Always construct the full path for AI analysis
                        var fullCvPath = Path.Combine(_environment.WebRootPath, "uploads", user.CVPath);
                        AIHelperRequest request = new AIHelperRequest
                        {
                            CommandId = 0, // Assuming 0 is for CV analysis
                            CVPath = fullCvPath,
                            User = user
                        };
                        string aiResponseText = await aiService.GetAIResponse(request);
                        var aiResponse = new UserAIResponse
                        {
                            UserId = user.Id,
                            responseText = aiResponseText,
                            createdAt = DateTime.UtcNow
                        };
                        dbContext.AIResponses.Add(aiResponse);
                        await dbContext.SaveChangesAsync();
                        LoggingConfigService.ReturnInfo("AI analysis completed and response saved.");
                    }
                    catch (Exception ex)
                    {
                        LoggingConfigService.ReturnError($"Background AI generation failed: {ex.Message}");
                        return;
                    }
                }
            });

            return (true, string.Empty);
        }
    }
}
