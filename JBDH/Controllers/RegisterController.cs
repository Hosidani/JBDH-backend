using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JBDH.Models;
using JBDH.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JBDH.Services.AICommunications;
using JBDH.Services.Notifications;
using JBDH.Services.UserManagement;
using JBDH.Services.Organisation;
using JBDH.Services.Configuration;
namespace JBDH.Controllers;

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
    public async Task<IActionResult> RegisterUser(RegisterUserViewModel model)
    {
        LoggingConfigService.ReturnInfo("RegisterUser method called.");
        var result = await _userRegistrationService.RegisterUserAsync(model, HttpContext, Url);
        if (!result.Success)
        {
            ViewData["ErrorMessage"] = result.ErrorMessage;
            return View(model);
        }
        ViewData["Email"] = model.Email;
        return View("CheckYourEmail");
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
        {
            ViewData["ErrorMessage"] = "You must be logged in to register an organisation.";
            return View();
        }

        var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

        if (!result.Success)
        {
            ViewData["ErrorMessage"] = result.ErrorMessage;
            return View();
        }

        ViewData["SuccessMessage"] = "Organisation registered successfully!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult VerifyEmail(string token)
    {
        var result = _userVerificationService.VerifyEmail(token);
        if (!result.Success)
        {
            ViewData["ErrorMessage"] = result.ErrorMessage;
            return View("VerificationFailed");
        }
        return View("VerificationSuccess");
    }

    [HttpPost]
    public async Task<IActionResult> ResendVerificationEmail(string email)
    {
        LoggingConfigService.ReturnInfo($"Resending verification email to {email}");
        var result = await _userVerificationService.ResendVerificationEmailAsync(email, Url, Request);
        if (!result.Success)
        {
            ViewData["ErrorMessage"] = result.ErrorMessage;
            ViewData["Email"] = email;
            return View("CheckYourEmail");
        }
        TempData["Email"] = email;
        return RedirectToAction("CheckYourEmail");
    }

    public IActionResult CheckYourEmail()
    {
        ViewData["Email"] = TempData["Email"];
        return View();
    }
}
