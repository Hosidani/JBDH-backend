using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using BCrypt.Net;

namespace JBDH.Controllers;

public class UserController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(User user)
    {
        if (ModelState.IsValid)
        {
            // Hash the password before saving
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            // Set default role as "User" if not provided
            if (string.IsNullOrEmpty(user.Role))
            {
                user.Role = "User";
            }

            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("Index", "Home");
        }
        return View(user);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string username, string password, string confirmPassword, string email)
    {
        Console.WriteLine($"Username: {username}");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Confirm Password: {confirmPassword}");
        Console.WriteLine($"Email: {email}");

        if (password != confirmPassword)
        {
            Console.WriteLine("Passwords do not match.");
            ViewData["ErrorMessage"] = "Passwords do not match.";
            return View();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Hash the password before saving
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                var user = new User
                {
                    Username = username,
                    PasswordHash = hashedPassword,
                    Role = "User", // Default role
                    Email = email
                };

                Console.WriteLine("User object created successfully.");

                _context.Users.Add(user);
                _context.SaveChanges();

                Console.WriteLine("User saved to database successfully.");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering user: {ex.Message}");
                ViewData["ErrorMessage"] = "An error occurred while registering the user.";
            }
        }
        else
        {
            Console.WriteLine("ModelState is invalid.");
        }

        return View();
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear(); // Clear the session
        return RedirectToAction("Index", "Home"); // Redirect to the homepage
    }

    public IActionResult Login()
    {
        ViewData["IsLoggedIn"] = HttpContext.Session.GetString("Username") != null;
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Example logic for login
        if (username == "testuser" && password == "password") // Replace with actual authentication logic
        {
            HttpContext.Session.SetString("Username", username);
            return RedirectToAction("Index", "Home");
        }

        ViewData["ErrorMessage"] = "Invalid username or password.";
        return View();
    }
}
