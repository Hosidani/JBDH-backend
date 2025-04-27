using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JBDH.Models;
using JBDH.Services;

namespace JBDH.Controllers;

public class RegisterController : Controller
{
    private readonly ApplicationDbContext _context;

    public RegisterController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string password, string confirmPassword, string email, IFormFile cvFile, string organisation, int organisationId)
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
                developerMode = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Always await SaveChangesAsync for better performance!

            // Set session to log in
            HttpContext.Session.SetString("Username", username);
            ViewData["IsLoggedIn"] = true;
            ViewData["Username"] = username;

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            LoggingConfigService.ReturnError($"Error registering user: {ex.Message}");
            ViewData["ErrorMessage"] = "An error occurred while registering the user.";
            return View();
        }
    }
}
