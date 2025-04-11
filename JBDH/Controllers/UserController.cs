using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace JBDH.Controllers;

public class UserController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string username, string password, string confirmPassword, string email, IFormFile cvFile, string organisation, int organisationId)
    {
        if (password != confirmPassword)
        {
            LoggingConfig.ReturnError("Passwords do not match.");
            ViewData["ErrorMessage"] = "Passwords do not match.";
            return View();
        }

        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                LoggingConfig.ReturnError($"ModelState Error: {error.ErrorMessage}");
            }
            LoggingConfig.ReturnError("ModelState is invalid.");
            return View();
        }

        try
        {
            // Hash the password before saving
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
                cvPath = Path.Combine(uploadsPath, cvFile.FileName);
                using (var stream = new FileStream(cvPath, FileMode.Create))
                {
                    cvFile.CopyTo(stream);
                }
                LoggingConfig.ReturnInfo($"CV uploaded to: {cvPath}");
            }

            // Create the user object
            var user = new User
            {
                Username = username,
                PasswordHash = hashedPassword,
                Role = "User", // Default role
                Email = email,
                CVPath = cvPath, // Save the CV path in the database
                Organisation = organisation ?? "Free", // Default organisation if null
                OrganisationId = organisationId,
                developerMode = false // Always set developerMode to false
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Set session to log the user in
            HttpContext.Session.SetString("Username", username);
            ViewData["IsLoggedIn"] = true;
            ViewData["Username"] = username;

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            LoggingConfig.ReturnError($"Error registering user: {ex.Message}");
            ViewData["ErrorMessage"] = "An error occurred while registering the user.";
            return View();
        }
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.SignOutAsync("CookieAuth");
        return RedirectToAction("Login");
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        LoggingConfig.ReturnInfo($"Login attempt: Username={username}");

        // Fetch the user from the database
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            LoggingConfig.ReturnError("User not found.");
            ViewData["ErrorMessage"] = "Invalid username or password.";
            return View();
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            LoggingConfig.ReturnError("PasswordHash is null or empty for the user.");
            ViewData["ErrorMessage"] = "Invalid username or password.";
            return View();
        }

        if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // If the user exists and the password is correct
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };
            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            HttpContext.SignInAsync("CookieAuth", claimsPrincipal);

            LoggingConfig.ReturnInfo($"User {username} logged in successfully.");
            return RedirectToAction("Index", "Home");
        }

        // If login fails
        ViewData["ErrorMessage"] = "Invalid username or password.";
        LoggingConfig.ReturnError("Invalid username or password.");
        return View();
    }
}
