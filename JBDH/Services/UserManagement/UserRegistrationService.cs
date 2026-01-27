using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.ViewModels;
using JBDH.Models.Services;
using JBDH.Models.Notifications;
using JBDH.Services.Notifications;
using JBDH.Services.AICommunications;
using JBDH.Services.Configuration;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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
        if (_context.Users.Any(u => u.Email == model.Email))
        {
            LoggingConfigService.ReturnInfo("This email is already registered.");
            return ServiceResult.Failure("This email is already registered.");
        }
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return new ServiceResult { Success = false, ErrorMessage = "Email is required" };
        }
        // Determine primary role based on selection
        string primaryRole = "User"; // Default
        
        if (model.SelectedRoles != null && model.SelectedRoles.Any())
        {
            // If user selected roles, use the first one as primary
            var selectedRole = model.SelectedRoles.First();
            if (selectedRole.Equals("Recruiter", StringComparison.OrdinalIgnoreCase) || 
                selectedRole.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                primaryRole = selectedRole;
            }
            // If they selected something else, keep "User" as primary
        }
        // If no roles selected, primaryRole stays "User"
        
        var user = new User
        {
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Email = model.Email,
            Location = model.Location ?? string.Empty,
            Searchable = model.Open,
            DeveloperMode = false,
            Role = primaryRole // Set the discriminator role (User, Recruiter, or Manager)
        };
        
        // Add ONLY the primary role to role assignments (no automatic "User" role)
        user.Roles.Add(new UserRoleAssignment { User = user, Role = primaryRole });
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

        // Create a default Account for the new user
        var account = new Account
        {
            UserId = user.Id,
            AccountName = user.Name ?? user.Email,
            AccountEmail = user.Email,
            AccountPhone = null,
            PreferredLanguage = model.PreferredLanguage ?? "en",
            AccountType = AccountType.Basic,
            AcceptedTermsAndConditions = false
        };
        _context.Accounts.Add(account);
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

        // Send verification email synchronously and handle errors
        string? emailError = null;
        try
        {
            LoggingConfigService.ReturnInfo($"Attempting to send verification email to {user.Email}");
            var verificationLink = urlHelper.Action("VerifyEmail", "Register", new { token }, httpContext.Request.Scheme);
            var emailResult = await _emailService.SendEmailAsync(user.Email, "Verify your email", $"Please verify your email by clicking this link: {verificationLink}");
            if (!string.Equals(emailResult, "Success", StringComparison.OrdinalIgnoreCase))
            {
                LoggingConfigService.ReturnError($"Failed to send verification email: {emailResult}");
                emailError = emailResult;
            }
            else
            {
                LoggingConfigService.ReturnInfo($"Verification email sent to {user.Email}");
            }
        }
        catch (Exception ex)
        {
            LoggingConfigService.ReturnError($"Exception sending verification email: {ex.Message}");
            emailError = ex.Message;
        }
        if (emailError != null)
        {
            return ServiceResult.Failure($"Registration succeeded, but failed to send verification email: {emailError}");
        }

        // Set session
        // httpContext.Session.SetString("Email", model.Email); // Uncomment if you want to store email in session

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
                    // Add AIUserCoursesPropsals service call
                    var aiUserCourseProposalService = scope.ServiceProvider.GetRequiredService<IAIUserCourseProposalService>();
                    await aiUserCourseProposalService.GenerateAndSaveCourseProposalAsync(userFromDb);
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