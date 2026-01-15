using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using JBDH.Services.UserManagement;
using JBDH.Services.Configuration;
using JBDH.Services.AICommunications;
using JBDH.Services.Notifications;
using JBDH.Services.Organisation;
using Microsoft.AspNetCore.Authorization;
using JBDH.Helpers;

namespace JBDH.Controllers
{

    [IncludeInApi]
    public class RegisterController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegisterController> _logger;
    private readonly IEmailService _emailService;
    private readonly IAIApiClient _aIApiClient;
    private readonly IUserRegistrationService _userRegistrationService;
    private readonly IRegisterOrganisationService _registerOrganisationService;
    private readonly IUserVerificationService _userVerificationService;
    private readonly IUserAIResponseService _userAIResponseService;

    public RegisterController(
        IEmailService emailService,
        ApplicationDbContext context,
        ILogger<RegisterController> logger,
        IAIApiClient aIApiClient,
        IUserRegistrationService userRegistrationService,
        IRegisterOrganisationService registerOrganisationService,
        IUserVerificationService userVerificationService,
        IUserAIResponseService userAIResponseService
    )
    {
        _emailService = emailService;
        _context = context;
        _logger = logger;
        _aIApiClient = aIApiClient;
        _userRegistrationService = userRegistrationService;
        _registerOrganisationService = registerOrganisationService;
        _userVerificationService = userVerificationService;
        _userAIResponseService = userAIResponseService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(); // Prompt to select registration type
    }

    [HttpGet]
    public IActionResult RegisterUser()
    {
        var model = new RegisterUserViewModel
        {
            AvailableRoles = new List<string> { "Manager", "Recruiter" }, // or whatever roles you support
            SelectedRoles = new List<string>() // initialize as empty list
        };
        return View(model); // View for user registration
    }

    [HttpPost]
    public async Task<IActionResult> RegisterUser([FromForm] RegisterUserViewModel model)
    {
        LoggingConfigService.ReturnInfo("RegisterUser method called.");
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Invalid registration data.", details = ModelState });
        }
        var result = await _userRegistrationService.RegisterUserAsync(model, HttpContext, Url);
        if (!result.Success)
        {
            // Return the actual error message from the service for easier debugging (including email errors)
            return Conflict(new { error = result.ErrorMessage });
        }
        return Ok(new { message = "Registration successful! Please check your email." });
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
        var managerUsername = User.Identity?.Name;
        if (string.IsNullOrEmpty(managerUsername))
            return this.ErrorPage("You must be logged in to register an organisation.");

        var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);
        if (!result.Success)
            return this.ErrorPage(result.ErrorMessage ?? "Could not register organisation.");

        return this.SetSuccessAndRedirect("Organisation registered successfully!", "Index", new { controller = "Home" });
    }

    [HttpGet]
    public IActionResult VerifyEmail(string token)
    {
        var result = _userVerificationService.VerifyEmail(token);
        if (!result.Success)
            return this.ErrorPage(result.ErrorMessage ?? "Email verification failed.");
        return this.SetSuccessAndRedirect("Email verified successfully!", "VerificationSuccess");
    }

    [HttpPost]
    public async Task<IActionResult> ResendVerificationEmail(string email)
    {
        LoggingConfigService.ReturnInfo($"Resending verification email to {email}");
        var result = await _userVerificationService.ResendVerificationEmailAsync(email, Url, Request);
        if (!result.Success)
        {
            this.SetErrorMessage(result.ErrorMessage ?? "Could not resend verification email.");
            ViewData["Email"] = email;
            return View("CheckYourEmail");
        }
        this.SetSuccessMessage("Verification email resent successfully.");
        TempData["Email"] = email;
        return this.RedirectTo("CheckYourEmail");
    }

    public IActionResult CheckYourEmail()
    {
        ViewData["Email"] = TempData["Email"];
        return View();
    }
}

}
