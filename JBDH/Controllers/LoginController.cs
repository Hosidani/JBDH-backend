using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using JBDH.Services.Authentication;
using JBDH.Services.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using JBDH.Helpers;

namespace JBDH.Controllers
{

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
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            HttpContext.Session.Clear();
            ViewData["Username"] = null;
            return this.RedirectToHome();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            LoggingConfigService.ReturnInfo($"Login attempt: Username={username}");
            var result = await _loginService.ValidateUserAsync(username, password);
            if (result.IsSuccess)
            {
                await _loginService.SignInUserAsync(username);
                LoggingConfigService.ReturnInfo($"User {username} logged in successfully.");
                return this.RedirectToHome();
            }
            this.SetErrorMessage(result.ErrorMessage ?? "Unknown error.");
            LoggingConfigService.ReturnError(result.ErrorMessage ?? "Unknown error.");
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If the user is already authenticated, redirect them to the home page (or any page you choose)
            if (User.Identity?.IsAuthenticated == true)
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
            this.SetSuccessMessage("If your email is in our system, a reset link has been sent.");
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
                this.SetErrorMessage("Passwords do not match.");
                return View(model);
            }

            var success = await _passwordResetService.ResetPasswordAsync(model.Token, model.NewPassword);
            if (!success)
            {
                this.SetErrorMessage("Invalid or expired token.");
                return View(model);
            }

            this.SetSuccessMessage("Password reset successfully.");
            return this.RedirectTo("Login");
        }
    }
}