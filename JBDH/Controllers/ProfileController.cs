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
    private readonly IFriendService _friendService; // Add reference to FriendService

    public ProfileController(ApplicationDbContext context, IProfileService profileService, IFriendService friendService)
    {
        _context = context;
        _profileService = profileService;
        _friendService = friendService; // Initialize FriendService
    }

    // GET: Profile
    [HttpGet]
    public async Task<IActionResult> Profile(int userId)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return View("ErrorPage", new ErrorViewModel { Message = "Invalid user context." });
        }

        if (currentUserId == userId)
        {
            LoggingConfigService.ReturnInfo($"User {currentUserId} tried to access their own profile.");
            return RedirectToAction("MyProfile");
        }

        var user = await _profileService.GetUserByIdAsync(userId);
        LoggingConfigService.ReturnInfo($"User {userId} profile accessed by {currentUserId}.");
        if (user == null)
        {
            return View("ErrorPage", new ErrorViewModel { Message = "User not found." });
        }

        var isFriend = await _friendService.AreFriendsAsync(currentUserId, userId);

        var viewModel = new ProfileViewModel
        {
            User = user,
            IsFriend = isFriend,
            Organization = user.IsOrganizationVerified && user.Organization != null ? user.Organization.Name : null // Display organization name only if verified and not null
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> MyProfile()
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return View("ErrorPage", new ErrorViewModel { Message = "Invalid user context." });
        }

        var user = await _profileService.GetUserByIdAsync(currentUserId);
        if (user == null)
        {
            return View("ErrorPage", new ErrorViewModel { Message = "User not found." });
        }

        var viewModel = new ProfileViewModel
        {
            User = user,
            IsFriend = false // Doesn't matter here, since it's the user's own profile
        };

        return View("MyProfile", viewModel); // Make sure you're explicitly referring to MyProfile.cshtml
    }

    // Method to send a friend request
    [HttpPost]
    public async Task<IActionResult> SendFriendRequest(int addresseeId)
    {
        var currentUserName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (currentUserName == null)
        {
            return RedirectToAction("Login", "Login");
        }

        // Parse the addresseeId
        var currentUserId = int.Parse(currentUserName);

        if (!int.TryParse(addresseeId.ToString(), out int addresseeIdInt))
        {
            return View("ErrorPage", "Invalid addressee ID.");
        }

        var success = await _friendService.SendFriendRequestAsync(currentUserId, addresseeIdInt);

        if (success)
        {
            return RedirectToAction("Profile", new { userId = addresseeId });
        }
        else
        {
            return View("ErrorPage", "Could not send friend request.");
        }
    }


}