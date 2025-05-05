using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Authorization;

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
    public async Task<IActionResult> RegisterUser(string username, string password, string confirmPassword, string email, IFormFile cvFile, bool openForHiring)
    {
        if (password != confirmPassword)
        {
            ViewData["ErrorMessage"] = "Passwords do not match.";
            return View();
        }

        if (_context.Users.Any(u => u.Username == username))
        {
            ViewData["ErrorMessage"] = "Username already exists.";
            return View();
        }

        try
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

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
            }

            var user = new User
            {
                Username = username,
                PasswordHash = hashedPassword,
                Role = "User",
                Email = email,
                CVPath = cvPath,
                DeveloperMode = false,
                OpenForHiring = openForHiring
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("Username", username);
            return RedirectToAction("Index", "Home");
        }
        catch
        {
            ViewData["ErrorMessage"] = "An error occurred while registering the user.";
            return View();
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
