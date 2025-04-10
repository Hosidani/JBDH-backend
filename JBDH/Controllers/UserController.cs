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
    public IActionResult Register(string username, string password, string confirmPassword, string email, IFormFile cvFile)
    {
        Console.WriteLine($"Username: {username}");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Confirm Password: {confirmPassword}");
        Console.WriteLine($"Email: {email}");
       
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email)|| cvFile.Length == 0)
        {
            Console.WriteLine("One or more required fields are empty.");
            ViewData["ErrorMessage"] = "All fields are required.";
            return View();
        }

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
                    Console.WriteLine($"CV uploaded to: {cvPath}");
                }
                var user = new User
                {
                    Username = username,
                    PasswordHash = hashedPassword,
                    Role = "User", // Default role
                    Email = email,
                    CVPath = cvPath // Save the CV path in the database
                };
                Console.WriteLine($"CV Path: {cvPath}");
                Console.WriteLine("User object created successfully.");

                _context.Users.Add(user);
                _context.SaveChanges();

                Console.WriteLine("User saved to database successfully.");

                // Set session to log the user in
                HttpContext.Session.SetString("Username", username);
                ViewData["IsLoggedIn"] = true;
                ViewData["Username"] = username;

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
        Console.WriteLine($"Login attempt: Username={username}");

        // Fetch the user from the database
        var user = _context.Users.FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            Console.WriteLine("User not found.");
            ViewData["ErrorMessage"] = "Invalid username or password.";
            return View();
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            Console.WriteLine("PasswordHash is null or empty for the user.");
            ViewData["ErrorMessage"] = "Invalid username or password.";
            return View();
        }

        if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // If the user exists and the password is correct
            HttpContext.Session.SetString("Username", username); // Store username in session
            ViewData["IsLoggedIn"] = true;
            ViewData["Username"] = username;
            Console.WriteLine($"Login successful for user: {username}");
            return RedirectToAction("Index", "Home");
        }

        // If login fails
        ViewData["ErrorMessage"] = "Invalid username or password.";
        Console.WriteLine($"Login failed for user: {username}");
        return View();
    }
}
