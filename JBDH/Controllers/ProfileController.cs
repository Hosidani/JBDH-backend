using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Friendship;
using JBDH.Models.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using JBDH.Models.Job;
using JBDH.Helpers;

namespace JBDH.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProfileService _profileService;
        private readonly IFriendService _friendService;
        private readonly IAIApiClient _AIApiClient;
        private readonly IUserAIResponseService _userAIResponseService;

        public ProfileController(
            IUserAIResponseService userAIResponseService,
            ApplicationDbContext context,
            IProfileService profileService,
            IFriendService friendService,
            IAIApiClient aIAApiClient)
        {
            _context = context;
            _profileService = profileService;
            _friendService = friendService;
            _AIApiClient = aIAApiClient;
            _userAIResponseService = userAIResponseService;
        }

        [HttpGet]
        public async Task<IActionResult> Profile(int userId)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            if (authResult.Value.HasValue && authResult.Value.Value == userId)
                return this.RedirectToMyProfile();

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            try
            {
                var viewModel = await _profileService.GetProfileViewModelAsync(authResult.Value.Value, userId);
                var currentUser = await _context.Users.FindAsync(authResult.Value.Value);
                ViewBag.CurrentUser = currentUser;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                return this.ErrorPage(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            var model = await _profileService.GetMyProfileViewModelAsync(authResult.Value.Value);
            var latestProposal = await _context.AIUserCourseProposals
                .Where(p => p.UserId == authResult.Value.Value)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestProposal != null)
                model.CourseProposals = latestProposal;
            // If CourseProposals is not nullable, this prevents null assignment.
            return View("MyProfile", model);
        }

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(int addresseeId)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect("User is not authenticated.");
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            var parseResult = this.ValidateAndHandleParseError(addresseeId.ToString(), "Invalid addressee ID.");
            if (parseResult.ErrorResult != null)
                return parseResult.ErrorResult;

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");
            if (!parseResult.Value.HasValue)
                return this.ErrorPage("Parsed addressee ID is missing.");

            var result = await _friendService.SendFriendRequestAsync(authResult.Value.Value, parseResult.Value.Value);
            if (result.Success)
            {
                return this.SetSuccessAndRedirect("Friend request sent!", "Profile", new { userId = addresseeId });
            }
            else
            {
                return this.ErrorPage(result.ErrorMessage ?? "Could not send friend request.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAboutMe(string aboutMe)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return authResult.ErrorResult;

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            var user = await _context.Users.FindAsync(authResult.Value.Value);
            if (user == null)
                return this.ErrorPage("User not found.");

            user.AboutMe = aboutMe;
            await _context.SaveChangesAsync();

            return this.SetSuccessAndRedirect("About Me updated!", "MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            await _friendService.AcceptFriendRequestAsync(friendshipId);
            return this.SetSuccessAndRedirect("Friend request accepted!", "MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineFriendRequest(int friendshipId)
        {
            await _friendService.DeclineFriendRequestAsync(friendshipId);
            return this.SetSuccessAndRedirect("Friend request declined.", "MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateJobApplicationNotes(int applicationId, string notes)
        {
            var app = await _context.Set<JobApplication>().FindAsync(applicationId);
            if (app == null)
                return this.ErrorPage("Job application not found.");

            app.Notes = notes;
            await _context.SaveChangesAsync();
            return this.SetSuccessAndRedirect("Notes updated!", "MyProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserFromProfile(int userId)
        {
            var authResult = this.EnsureAuthenticatedUserOrRedirect();
            if (authResult.ErrorResult != null)
                return Unauthorized();

            if (!authResult.Value.HasValue)
                return this.ErrorPage("Authenticated user ID is missing.");

            var currentUser = await _context.Users.FindAsync(authResult.Value.Value);
            if (currentUser == null || currentUser.DeveloperMode != true)
                return this.ForbidWithMessage("You do not have permission to perform this action.");

            if (currentUser.Id == userId)
                return this.BadRequestMessage("You cannot delete yourself from this action.");

            var userToDelete = await _context.Users.FindAsync(userId);
            if (userToDelete == null)
                return this.ErrorPage("User not found.");

            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();

            return this.SetSuccessAndRedirect("User deleted successfully.", "Index", new { controller = "Home" });
        }
    }
}