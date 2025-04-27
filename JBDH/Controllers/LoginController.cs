using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;

namespace JBDH.Controllers;
public class LoginController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILoginService _loginService;
    public LoginController(ILoginService loginService)
    {
        _loginService = loginService;
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        LoggingConfigService.ReturnInfo($"Login attempt: Username={username}");
        // Validate the user using the service
        bool isValidUser = await _loginService.ValidateUserAsync(username, password);
        if (isValidUser)
        {
            // If valid, sign in the user
            await _loginService.SignInUserAsync(username);
            LoggingConfigService.ReturnInfo($"User {username} logged in successfully.");
            return RedirectToAction("Index", "Home");
        }
        // If login fails
        ViewData["ErrorMessage"] = "Invalid username or password.";
        LoggingConfigService.ReturnError("Invalid username or password.");
        return View();
    }

    [HttpGet]
    public IActionResult Login()
    {
        // If the user is already authenticated, redirect them to the home page (or any page you choose)
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(); // Render the login page
    }
}