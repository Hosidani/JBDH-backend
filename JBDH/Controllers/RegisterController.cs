using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JBDH.Models;
using JBDH.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Controllers;

public class RegisterController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegisterController> _logger;
    private readonly IEmailService _emailService;
    
    private readonly IAIService _aiService;

    public RegisterController(IEmailService emailService, ApplicationDbContext context, ILogger<RegisterController> logger, IAIService aiService)
    {
        _emailService = emailService;
        _context = context;
        _logger = logger;
        _aiService = aiService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(); // Prompt to select registration type
    }

    [HttpGet]
    public IActionResult RegisterUser()
    {
        return View(); // View for user registration
    }

    [HttpPost]
    public async Task<IActionResult> RegisterUser(RegisterUserViewModel model)
    {
        LoggingConfigService.ReturnInfo("RegisterUser method called.");
        if (model.Password != model.ConfirmPassword)
        {
            LoggingConfigService.ReturnInfo("Passwords do not match.");
            ViewData["ErrorMessage"] = "Passwords do not match.";
            return View(model);
        }

        if (_context.Users.Any(u => u.Username == model.Username))
        {
            LoggingConfigService.ReturnInfo("Username already exists.");
            ViewData["ErrorMessage"] = "Username already exists.";
            return View(model);
        }
        // Check if the email is already registered
        if (_context.Users.Any(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "This email is already registered.");
            return View();
        }

        try
        {
            LoggingConfigService.ReturnInfo("Creating new user object.");
            var user = new User
            {
                Username = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Email = model.Email,
                OpenForHiring = model.OpenForHiring
            };
            LoggingConfigService.ReturnInfo("User object created.");
            LoggingConfigService.ReturnInfo("Checking if CV file is provided.");
            if (model.CVFile != null && model.CVFile.Length > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }
                var safeFileName = Path.GetFileName(model.CVFile.FileName);
                user.CVPath = Path.Combine(uploadsPath, safeFileName);
                using (var stream = new FileStream(user.CVPath, FileMode.Create))
                {
                    await model.CVFile.CopyToAsync(stream);
                }
            }
            LoggingConfigService.ReturnInfo("CV file processed.");
            user.DeveloperMode = false; // Default value
            user.Role = "User"; // Default role
            LoggingConfigService.ReturnInfo("User role set to User.");

            _context.Users.Add(user);
            LoggingConfigService.ReturnInfo("User object added to context.");
            await _context.SaveChangesAsync(); // Always await SaveChangesAsync for better performance!
            LoggingConfigService.ReturnInfo("Changes saved to database.");
            // Generate verification token
            var token = Guid.NewGuid().ToString();
            var emailVerificationToken = new JBDH.Models.EmailVerificationToken
            {
                Token = token,
                UserId = user.Id,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                IsUsed = false
            };
            LoggingConfigService.ReturnInfo("Email verification token generated.");
            // Save the token to the database
            _context.EmailVerificationTokens.Add(emailVerificationToken);
            await _context.SaveChangesAsync();
            LoggingConfigService.ReturnInfo("Email verification token saved to database.");
            // Send verification email
            try
            {
                var verificationLink = Url.Action("VerifyEmail", "Register", new { token }, Request.Scheme);
                await _emailService.SendEmailAsync(user.Email, "Verify your email", $"Please verify your email by clicking this link: {verificationLink}");
                _logger.LogInformation("Verification email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email to {Email}", user.Email);
                ModelState.AddModelError("", "There was an error sending the verification email. Please try again.");
                return View(user);
            }
            LoggingConfigService.ReturnInfo("Verification email sent.");
            // Set session to log in
            HttpContext.Session.SetString("Username", model.Username);
            ViewData["IsLoggedIn"] = true;
            ViewData["Username"] = model.Username;

            ViewBag.Email = model.Email;
            var aiResponse = await _aiService.GetAIResponse(user, 0);
            var viewModel = new ProfileViewModel
            {
                User = user,
                IsFriend = false, // Doesn't matter here, since it's the user's own profile
                AIResponse = aiResponse
            };
            return View("CheckYourEmail");
        }
        catch
        {
            ViewData["ErrorMessage"] = "An error occurred while registering the user.";
            return View(model);
        }
    }

    [Authorize(Roles = "Manager")]
    [HttpGet]
    public IActionResult RegisterOrganization()
    {
        return View(); // View for organization registration
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> RegisterOrganization(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            ViewData["ErrorMessage"] = "Organization name is required.";
            return View();
        }

        try
        {
            var managerUsername = User.Identity?.Name;
            var manager = await _context.Managers.FirstOrDefaultAsync(m => m.Username == managerUsername);

            if (manager == null)
            {
                ViewData["ErrorMessage"] = "You must be a manager to register an organization.";
                return View();
            }

            // Access ManagedOrganizations from the manager object
            var organization = new Organization
            {
                Name = name,
                Managers = new List<User> { manager }
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            ViewData["SuccessMessage"] = "Organization registered successfully!";
            return RedirectToAction("Index", "Home");
        }
        catch
        {
            ViewData["ErrorMessage"] = "An error occurred while registering the organization.";
            return View();
        }
    }

    [HttpGet]
    public IActionResult VerifyEmail(string token)
    {
        // Find the token in the EmailVerificationTokens table
        var emailVerificationToken = _context.EmailVerificationTokens
            .FirstOrDefault(t => t.Token == token && !t.IsUsed);

        if (emailVerificationToken == null || emailVerificationToken.ExpirationDate <= DateTime.UtcNow)
        {
            ViewBag.ErrorMessage = "The verification link is invalid or has expired. Please request a new verification email.";
            return View("VerificationFailed");
        }

        // Mark the token as used
        emailVerificationToken.IsUsed = true;

        // Mark the user's email as confirmed
        var user = _context.Users.FirstOrDefault(u => u.Id == emailVerificationToken.UserId);

        _context.SaveChanges();

        return View("VerificationSuccess");
    }
    // Resend Verification Email
    [HttpGet]
    public async Task<IActionResult> ResendVerificationEmail(string email)
    {
        LoggingConfigService.ReturnInfo($"Resending verification email to {email}");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        var token = Guid.NewGuid().ToString();
        var emailVerificationToken = new JBDH.Models.EmailVerificationToken
        {
            Token = token,
            UserId = user.Id,
            ExpirationDate = DateTime.UtcNow.AddDays(1),
            IsUsed = false
        };
        LoggingConfigService.ReturnInfo($"Generated new token: {token}");
        _context.EmailVerificationTokens.Add(emailVerificationToken);
        await _context.SaveChangesAsync();

        LoggingConfigService.ReturnInfo($"Token saved to database for user {user.Username}");
        var verificationLink = Url.Action("VerifyEmail", "Register", new { token }, Request.Scheme);
        await _emailService.SendEmailAsync(user.Email, "Verify your email", $"Please verify your email by clicking this link: {verificationLink}");

        ViewBag.Email = email;
        return View("CheckYourEmail");
    }
    
}
