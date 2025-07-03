using System.Security.Claims;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Friendship;
using JBDH.Models.ViewModels;
using JBDH.Models.User;
using JBDH.Models.Core;
using JBDH.Services.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using JBDH.Models.Job;

namespace JBDH.Controllers
{

    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProfileService _profileService;
        private readonly IFriendService _friendService; // Add reference to FriendService
        private readonly IAIApiClient _AIApiClient;

        private readonly IUserAIResponseService _userAIResponseService; // Add reference to IUserAIResponseService


        public ProfileController(IUserAIResponseService userAIResponseService, ApplicationDbContext context, IProfileService profileService, IFriendService friendService, IAIApiClient aIAApiClient)
        {
            _context = context;
            _profileService = profileService;
            _friendService = friendService; // Initialize FriendService
            _AIApiClient = aIAApiClient;
            _userAIResponseService = userAIResponseService; // Initialize IUserAIResponseService
        }

        // GET: Profile
        [HttpGet]
        public async Task<IActionResult> Profile(int userId)
        {
            var currentUserId = _profileService.GetCurrentUserId(User);
            if (currentUserId == null)
                return View("ErrorPage", new ErrorViewModel { Message = "Invalid user context." });

            if (currentUserId == userId)
                return RedirectToAction("MyProfile");

            try
            {
                var viewModel = await _profileService.GetProfileViewModelAsync(currentUserId.Value, userId);
                var currentUser = await _context.Users.FindAsync(currentUserId.Value);
                ViewBag.CurrentUser = currentUser;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("ErrorPage", new ErrorViewModel { Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var currentUserId = _profileService.GetCurrentUserId(User);
            if (currentUserId == null)
                return View("ErrorPage", new ErrorViewModel { Message = "Invalid user context." });

            // Use your service to get the full view model
            var model = await _profileService.GetMyProfileViewModelAsync(currentUserId.Value);

            return View("MyProfile", model);
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

            var currentUserId = int.Parse(currentUserName);

            if (!int.TryParse(addresseeId.ToString(), out int addresseeIdInt))
            {
                return View("ErrorPage", new ErrorViewModel { Message = "Invalid addressee ID." });
            }

            var result = await _friendService.SendFriendRequestAsync(currentUserId, addresseeIdInt);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Friend request sent!";
                return RedirectToAction("Profile", new { userId = addresseeId });
            }
            else
            {
                return View("ErrorPage", result.ErrorMessage ?? "Could not send friend request.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAboutMe(string aboutMe)
        {
            var currentUserId = _profileService.GetCurrentUserId(User);
            if (currentUserId == null)
                return View("ErrorPage", new ErrorViewModel { Message = "Invalid user context." });

            var user = await _context.Users.FindAsync(currentUserId.Value);
            if (user == null)
                return View("ErrorPage", new ErrorViewModel { Message = "User not found." });

            user.AboutMe = aboutMe;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "About Me updated!";
            return RedirectToAction("MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            await _friendService.AcceptFriendRequestAsync(friendshipId);
            TempData["SuccessMessage"] = "Friend request accepted!";
            return RedirectToAction("MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineFriendRequest(int friendshipId)
        {
            await _friendService.DeclineFriendRequestAsync(friendshipId);
            TempData["SuccessMessage"] = "Friend request declined.";
            return RedirectToAction("MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateJobApplicationNotes(int applicationId, string notes)
        {
            var app = await _context.Set<JobApplication>().FindAsync(applicationId);
            if (app == null)
            {
                TempData["ErrorMessage"] = "Job application not found.";
                return RedirectToAction("MyProfile");
            }
            app.Notes = notes;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notes updated!";
            return RedirectToAction("MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserFromProfile(int userId)
        {
            // Get current user
            var currentUserId = _profileService.GetCurrentUserId(User);
            if (currentUserId == null)
                return Unauthorized();

            var currentUser = await _context.Users.FindAsync(currentUserId.Value);
            if (currentUser == null || currentUser.DeveloperMode != true)
                return Forbid();

            // Prevent self-deletion here if you want
            if (currentUser.Id == userId)
                return BadRequest("You cannot delete yourself from this action.");

            // Reuse the logic from SettingsController (or call the service if you moved it)
            var userToDelete = await _context.Users.FindAsync(userId);
            if (userToDelete == null)
                return NotFound();

            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "User deleted successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}