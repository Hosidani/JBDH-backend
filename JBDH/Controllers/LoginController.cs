using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using Microsoft.EntityFrameworkCore;
namespace JBDH.Controllers;

public class LoginController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILoginService _loginService;
    private readonly IPasswordResetService _passwordResetService;
    public LoginController(ApplicationDbContext context, ILoginService loginService, IPasswordResetService passwordResetService)
    {
        _context = context;
        _loginService = loginService;
        _passwordResetService = passwordResetService;
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        LoggingConfigService.ReturnInfo($"Login attempt: Username={username}");
        // Validate the user using the service
        var result = await _loginService.ValidateUserAsync(username, password);
        if (result.IsSuccess)
        {
            // If valid, sign in the user
            await _loginService.SignInUserAsync(username);
            LoggingConfigService.ReturnInfo($"User {username} logged in successfully.");
            return RedirectToAction("Index", "Home");
        }
        // If login fails
        ViewData["ErrorMessage"] = result.ErrorMessage;
        LoggingConfigService.ReturnError(result.ErrorMessage);
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

    [HttpGet]
    public IActionResult ForgetPassword()
    {
        return View(); // Show the email input form
    }

    [HttpPost]
    public async Task<IActionResult> ForgetPassword(string email)
    {
        await _passwordResetService.RequestPasswordResetAsync(email);
        ViewData["Message"] = "If your email is in our system, a reset link has been sent.";
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Invalid reset token.");
        }

        var model = new ResetPasswordViewModel { Token = token };
        return View(model); // Show form with hidden token field
    }
    
    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid || model.NewPassword != model.ConfirmPassword)
        {
            ViewData["ErrorMessage"] = "Passwords do not match.";
            return View(model);
        }

        var success = await _passwordResetService.ResetPasswordAsync(model.Token, model.NewPassword);
        if (!success)
        {
            ViewData["ErrorMessage"] = "Invalid or expired token.";
            return View(model);
        }

        ViewData["SuccessMessage"] = "Password reset successfully.";
        return RedirectToAction("Login");
    }



}