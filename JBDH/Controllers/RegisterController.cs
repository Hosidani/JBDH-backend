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
    private readonly IAIResponseService _aiResponseService;
    private readonly IUserRegistrationService _userRegistrationService;

    public RegisterController(IEmailService emailService, ApplicationDbContext context, ILogger<RegisterController> logger, IAIService aiService, IAIResponseService aiResponseService, IUserRegistrationService userRegistrationService)
    {
        _emailService = emailService;
        _context = context;
        _logger = logger;
        _aiService = aiService;
        _aiResponseService = aiResponseService;
        _userRegistrationService = userRegistrationService;
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
        var result = await _userRegistrationService.RegisterUserAsync(model, HttpContext, Url);
        if (!result.Success)
        {
            ViewData["ErrorMessage"] = result.ErrorMessage;
            return View(model);
        }
        return View("CheckYourEmail");
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
