using JBDH.Services;
using JBDH.Models;
using JBDH.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using JBDH.Services.AICommunications.Interfaces;

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

        // Always assign "User" role
        user.Roles.Add(new UserRoleAssignment { User = user, Role = "User" });

        // Assign additional roles if selected
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

        // Handle CV upload
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

        // Generate AI response in background
        _ = Task.Run(async () =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var aiService = scope.ServiceProvider.GetRequiredService<IAIApiClient>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    // Only proceed if the user has a CVPath
                    if (string.IsNullOrEmpty(user.CVPath))
                    {
                        LoggingConfigService.ReturnInfo($"No CV found for user ID {user.Id}, skipping AI analysis.");
                        return;
                    }

                    AIHelperRequest request = new AIHelperRequest
                    {
                        User = user,
                        CommandId = 0, // Assuming 0 is for CV analysis
                        CVPath = user.CVPath
                    };
                    string aiResponseText = await aiService.GetAIResponse(request);
                    var userId = user.Id;
                    var userFromDb = await dbContext.Users.FindAsync(userId);
                    if (userFromDb == null)
                    {
                        LoggingConfigService.ReturnError($"User with ID {userId} not found in background AI generation.");
                        return;
                    }

                    UserAIResponse useraiResponse = new UserAIResponse
                    {
                        ResponseText = aiResponseText,
                        CreatedAt = DateTime.UtcNow,
                        UserId = userId,
                        User = userFromDb
                    };
                    _userAIResponseService.SaveUserAIResponse(useraiResponse, userFromDb);
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