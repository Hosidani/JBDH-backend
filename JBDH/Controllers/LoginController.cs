using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using JBDH.Services.Authentication;
using JBDH.Swagger;
using JBDH.Services.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using JBDH.Helpers;

namespace JBDH.Controllers
{
        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
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
            LoggingConfigService.ReturnInfo("[LoginController] Logout GET called");
            await HttpContext.SignOutAsync("CookieAuth");
            HttpContext.Session.Clear();
            ViewData["Username"] = null;
            return this.RedirectToHome();
        }

    [HttpPost]
    [Route("Login")]
    [IncludeInApi]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            LoggingConfigService.ReturnInfo($"[LoginController] Login POST called. Email={request?.Email}");
            var result = await _loginService.ValidateUserAsync(request?.Email, request?.Password);
            if (result.IsSuccess)
            {
                await _loginService.SignInUserAsync(request?.Email);
                LoggingConfigService.ReturnInfo($"User {request?.Email} logged in successfully.");
                return Ok(new { message = "Login successful" });
            }
            LoggingConfigService.ReturnError(result.ErrorMessage ?? "Unknown error.");
            return Unauthorized(new { error = result.ErrorMessage ?? "Unknown error." });
        }

    [HttpGet]
    public IActionResult Index()
        {
            LoggingConfigService.ReturnInfo($"[LoginController] Index GET called. Authenticated={User.Identity?.IsAuthenticated}");
            if (User.Identity?.IsAuthenticated == true)
            {
                LoggingConfigService.ReturnInfo("[LoginController] Index GET: Redirecting to Home/Index");
                return RedirectToAction("Index", "Home");
            }
            LoggingConfigService.ReturnInfo("[LoginController] Index GET: Rendering Login view");
            return View("Login"); // Render the login page
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