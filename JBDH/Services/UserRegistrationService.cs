using JBDH.Services;
using JBDH.Models;
using Microsoft.AspNetCore.Mvc;

public class UserRegistrationService : IUserRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAIService _aiService;
    private readonly IAIResponseService _aiResponseService;
    private readonly IServiceProvider _serviceProvider;


    public UserRegistrationService(
        ApplicationDbContext context,
        IEmailService emailService,
        IAIService aiService,
        IAIResponseService aiResponseService,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _emailService = emailService;
        _aiService = aiService;
        _aiResponseService = aiResponseService;
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterUserAsync(RegisterUserViewModel model, HttpContext httpContext, IUrlHelper urlHelper)
    {
        LoggingConfigService.ReturnInfo("RegisterUser method called.");
        if (model.Password != model.ConfirmPassword)
        {
            LoggingConfigService.ReturnInfo("Passwords do not match.");
            return (false, "Passwords do not match.");
        }

        if (_context.Users.Any(u => u.Username == model.Username))
        {
            LoggingConfigService.ReturnInfo("Username already exists.");
            return (false, "Username already exists.");
        }

        if (_context.Users.Any(u => u.Email == model.Email))
        {
            LoggingConfigService.ReturnInfo("This email is already registered.");
            return (false, "This email is already registered.");
        }

        var user = new User
        {
            Username = model.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Email = model.Email,
            OpenForHiring = model.OpenForHiring,
            DeveloperMode = false,
            Role = "User"
        };

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
        Task.Run(async () =>
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
        Task.Run(async () =>
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();
                var aiResponseService = scope.ServiceProvider.GetRequiredService<IAIResponseService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                try
                {
                    AIHelperRequest request = new AIHelperRequest
                    {
                        User = user,
                        CommandId = 0, // Assuming 0 is for CV analysis
                        CVPath = user.CVPath
                    };
                    string aiResponseText = await aiService.GetAIResponse(request);
                    UserAIResponse useraiResponse = new UserAIResponse
                    {
                        responseText = aiResponseText,
                        createdAt = DateTime.UtcNow,
                        UserId = user.Id
                    };
                    aiResponseService.SaveAIResponse(useraiResponse, user);
                }
                catch (Exception ex)
                {
                    LoggingConfigService.ReturnError($"Background AI generation failed: {ex.Message}");
                }
            }
        });

        return (true, null);
    }
}