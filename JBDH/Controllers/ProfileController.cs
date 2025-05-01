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

    [HttpGet]
    public async Task<IActionResult> Profile(int userId)
    {
        
        var currentUserName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserName == userId.ToString())
        {
            return RedirectToAction("MyProfile", "Profile");
        }
        
        // Parse the current user's ID from the claims
        var currentUserId = int.Parse(currentUserName);

        // Fetch user details by userId
        var user = await _profileService.GetUserByIdAsync(userId);

        // Check if current user and viewed user are friends
        var isFriend = await _friendService.AreFriendsAsync(currentUserId, userId);

        var viewModel = new ProfileViewModel
        {
            User = user,
            IsFriend = isFriend
        };

        return View(viewModel);
    }

    // Method to send a friend request
    [HttpPost]
    public async Task<IActionResult> SendFriendRequest(string addresseeId)

    {
        var currentUserName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (currentUserName == null)
        {
            return RedirectToAction("Login", "Login");
        }
        // Parse the current user's ID from the claims
        var currentUserId = int.Parse(currentUserName);
        
        // Parse the addresseeId to an integer
        if (!int.TryParse(addresseeId, out int addresseeIdInt))
        {
            return View("ErrorPage", new { message = "Invalid addressee ID." });
        }

        var success = await _friendService.SendFriendRequestAsync(currentUserId, addresseeIdInt);

        if (success)
        {
            return RedirectToAction("Profile", new { userId = addresseeId });
        }
        else
        {
            return View("ErrorPage", new { message = "Could not send friend request." });
        }
    }

}