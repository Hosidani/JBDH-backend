using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;



namespace JBDH.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IProfileService _profileService;

    public ProfileController(ApplicationDbContext context, IProfileService profileService)
    {
        _context = context;
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        LoggingConfigService.ReturnInfo($"Profile method called by {User.Identity.Name} ");
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return RedirectToAction("Login", "Login");
        }

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return RedirectToAction("Login", "Login");
        }
        LoggingConfigService.ReturnInfo($"User profile loaded successfully.");
        var user = await _profileService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }
        
        return View(user);
    }

}