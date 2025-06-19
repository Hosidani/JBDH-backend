using JBDH.Services;
using JBDH.Models;
using JBDH.Models.User;
using JBDH.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using JBDH.Services.Notifications;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;

namespace JBDH.Services.UserManagement
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAIApiClient _aIApiClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserAIResponseService _userAIResponseService;

    public UserRegistrationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IAIApiClient aIApiClient,
        IServiceProvider serviceProvider,
        IUserAIResponseService userAIResponseService)
    {
        _context = context;
        _emailService = emailService;
        _aIApiClient = aIApiClient;
        _serviceProvider = serviceProvider;
        _userAIResponseService = userAIResponseService;
    }

    private string ExtractJsonFromCodeBlock(string aiResponseText)
    {
        if (string.IsNullOrWhiteSpace(aiResponseText))
            return aiResponseText;

        var trimmed = aiResponseText.Trim();
        if (trimmed.StartsWith("```"))
        {
            // Remove the first line (```json or ```)
            var lines = trimmed.Split('\n').ToList();
            lines.RemoveAt(0);
            // Remove the last line if it's ```
            if (lines.Count > 0 && lines[^1].Trim().StartsWith("```"))
                lines.RemoveAt(lines.Count - 1);
            return string.Join("\n", lines).Trim();
        }
        return aiResponseText;
    }
    public async Task<ServiceResult> RegisterUserAsync(RegisterUserViewModel model, HttpContext httpContext, IUrlHelper urlHelper)
    {
        LoggingConfigService.ReturnInfo("RegisterUser method called.");
        if (model.Password != model.ConfirmPassword)
        {
            LoggingConfigService.ReturnInfo("Passwords do not match.");
            return ServiceResult.Failure("Passwords do not match.");
        }
        if (_context.Users.Any(u => u.Username == model.Username))
        {
            LoggingConfigService.ReturnInfo("Username already exists.");
            return ServiceResult.Failure("Username already exists.");
        }
        if (_context.Users.Any(u => u.Email == model.Email))
        {
            LoggingConfigService.ReturnInfo("This email is already registered.");
            return ServiceResult.Failure("This email is already registered.");
        }
        var user = new User
        {
            Username = model.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Email = model.Email,
            Location = model.Location,
            OpenForHiring = model.OpenForHiring,
            DeveloperMode = false,
        };
        user.Roles.Add(new UserRoleAssignment { User = user, Role = "User" });
        if (model.SelectedRoles != null)
        {
            foreach (var role in model.SelectedRoles.Distinct())
            {
                if (!string.Equals(role, "User", StringComparison.OrdinalIgnoreCase)) // Avoid duplicate
                {
                    user.Roles.Add(new UserRoleAssignment { User = user, Role = role });
                }
            }
        }
        if (model.CVFile != null && model.CVFile.Length > 0)
        {
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var safeFileName = Path.GetFileName(model.CVFile.FileName);
            user.CVPath = Path.Combine(uploadsPath, safeFileName);
            using (var stream = new FileStream(user.CVPath, FileMode.Create))
            {
                await model.CVFile.CopyToAsync(stream);
            }
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate and save verification token
        var token = Guid.NewGuid().ToString();
        var emailVerificationToken = new EmailVerificationToken
        {
            Token = token,
            UserId = user.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1),
            IsUsed = false
        };
        _context.EmailVerificationTokens.Add(emailVerificationToken);
        await _context.SaveChangesAsync();

        // Send verification email in background
        _ = Task.Run(async () =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                try
                {
                    var verificationLink = urlHelper.Action("VerifyEmail", "Register", new { token }, httpContext.Request.Scheme);
                    await emailService.SendEmailAsync(user.Email, "Verify your email", $"Please verify your email by clicking this link: {verificationLink}");
                }
                catch (Exception ex)
                {
                    LoggingConfigService.ReturnError($"Error sending verification email: {ex.Message}");
                }
            }
        });

        // Set session
        httpContext.Session.SetString("Username", model.Username);

        // Generate AI response in background using shared service
        _ = Task.Run(async () =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cvAnalysisService = scope.ServiceProvider.GetRequiredService<ICVAnalysisService>();
                    var userId = user.Id;

                    var userFromDb = await dbContext.Users.FindAsync(userId);
                    if (userFromDb == null)
                    {
                        LoggingConfigService.ReturnError($"User with ID {userId} not found in background AI generation.");
                        return;
                    }

                    if (string.IsNullOrEmpty(userFromDb.CVPath))
                    {
                        LoggingConfigService.ReturnInfo($"No CV found for user ID {userId}, skipping AI analysis.");
                        return;
                    }

                    await cvAnalysisService.AnalyzeAndSaveCVAsync(userFromDb, userFromDb.CVPath);
                }
                catch (Exception ex)
                {
                    LoggingConfigService.ReturnError($"Background AI generation failed: {ex.Message}");
                }
            }
        });

        return ServiceResult.SuccessResult();
    }
    }
}