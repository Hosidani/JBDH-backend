using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JBDH.Models;
using JBDH.Services;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Controllers;

public class RegisterController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegisterController> _logger;
    private readonly IEmailService _emailService;

    public RegisterController(IEmailService emailService, ApplicationDbContext context, ILogger<RegisterController> logger)
    {
        _emailService = emailService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string password, string confirmPassword, string email, IFormFile cvFile, string organisation, int organisationId, bool openForHiring)
    {
        if (password != confirmPassword)
        {
            LoggingConfigService.ReturnError("Passwords do not match.");
            ViewData["ErrorMessage"] = "Passwords do not match.";
            return View();
        }

        if (_context.Users.Any(u => u.Username == username))
        {
            LoggingConfigService.ReturnError("Username already exists.");
            ViewData["ErrorMessage"] = "Username already exists.";
            return View();
        }
        // Check if the email is already registered
        if (_context.Users.Any(u => u.Email == email))
        {
            ModelState.AddModelError("Email", "This email is already registered.");
            return View();
        }

        try
        {
            // Hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Handle CV upload
            string cvPath = null;
            if (cvFile != null && cvFile.Length > 0)
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }
                var safeFileName = Path.GetFileName(cvFile.FileName);
                cvPath = Path.Combine(uploadsPath, safeFileName);
                using (var stream = new FileStream(cvPath, FileMode.Create))
                {
                    await cvFile.CopyToAsync(stream);
                }
                LoggingConfigService.ReturnInfo($"CV uploaded to: {cvPath}");
            }

            // Create user
            var user = new User
            {
                Username = username,
                PasswordHash = hashedPassword,
                Role = "User",
                Email = email,
                CVPath = cvPath,
                Organisation = organisation ?? "Free",
                OrganisationId = organisationId,
                developerMode = false,
                openForHiring = openForHiring
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Always await SaveChangesAsync for better performance!

            // Generate verification token
            var token = Guid.NewGuid().ToString();
            var emailVerificationToken = new JBDH.Models.EmailVerificationToken
            {
                Token = token,
                UserId = user.Id,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                IsUsed = false
            };

            // Save the token to the database
            _context.EmailVerificationTokens.Add(emailVerificationToken);
            await _context.SaveChangesAsync();

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

            // Set session to log in
            HttpContext.Session.SetString("Username", username);
            ViewData["IsLoggedIn"] = true;
            ViewData["Username"] = username;

            ViewBag.Email = email;
            return View("CheckYourEmail");
        }
        catch (Exception ex)
        {
            LoggingConfigService.ReturnError($"Error registering user: {ex.Message}");
            ViewData["ErrorMessage"] = "An error occurred while registering the user.";
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
        if (user != null)
        {
            user.IsEmailConfirmed = true;
        }

        _context.SaveChanges();

        return View("VerificationSuccess");
    }
    // Resend Verification Email
    [HttpGet]
    public async Task<IActionResult> ResendVerificationEmail(string email)
    {
        LoggingConfigService.ReturnInfo($"Resending verification email to {email}");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || user.IsEmailConfirmed)
        {
            return View("VerificationFailed");
        }

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
