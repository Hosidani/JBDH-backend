using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace JBDH.Controllers;
public class LoginController : Controller
{
    private readonly ApplicationDbContext _context;
    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Query the database for the user
        var user = _context.Users
            .FirstOrDefault(u => u.Username == username && u.PasswordHash == password);

        if (user != null)
        {
            // Set session or authentication cookie
            HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
            return RedirectToAction("Index", "Home");
        }

        // Return an error message if login fails
        ViewData["ErrorMessage"] = "Invalid username or password.";
        return View();
    }
}