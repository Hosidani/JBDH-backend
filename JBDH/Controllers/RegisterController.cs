using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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
        LoggingConfigService.ReturnInfo("Starting insertion of user.");
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
            user.IsOrganizationVerified = false; // Default value
            user.Role = "User"; // Default role
            LoggingConfigService.ReturnInfo("User role set to User.");

            _context.Users.Add(user);
            LoggingConfigService.ReturnInfo("User object added to context.");
            await _context.SaveChangesAsync();
            LoggingConfigService.ReturnInfo("Changes saved to database.");

            HttpContext.Session.SetString("Username", user.Username);
            LoggingConfigService.ReturnInfo($"User {user.Username} registered successfully.");
            return RedirectToAction("Index", "Home");
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
                Managers = new List<Manager> { manager }
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
}
