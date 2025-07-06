using JBDH.Models.ViewModels;
using JBDH.Services;
using JBDH.Services.JobManagement;
using JBDH.Models.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JBDH.Models.Messaging;
using JBDH.Services.Friendship;
using JBDH.Services.Configuration;

namespace JBDH.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly JobService _jobService;
        private readonly IFriendService _friendService;
        public JobsController(ApplicationDbContext context, JobService jobService, IFriendService friendService)
        {
            _context = context;
            _jobService = jobService;
            _friendService = friendService;
        }

        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
                return NotFound();

            bool hasApplied = false;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                // Check if the user has already applied for this job
                hasApplied = await _context.JobApplications
                    .AnyAsync(a => a.JobId == id && a.UserId.ToString() == userId);
            }
            // Get the latest AI response for this job
            var latestAIResponse = await _context.JobAIResponses
                .Where(r => r.JobId == id)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            var model = new JobDetailsViewModel
            {
                Job = job,
                JobId = id,
                HasApplied = hasApplied,
                // Add this if you want to show the AI response in the details view:
                // LatestAIResponse = latestAIResponse
            };

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Conver currentUserId to int
            if (!int.TryParse(currentUserId, out int currentUserIdInt))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToAction("Index", "Home");
            }
            var friends = await _friendService.GetFriendsAsync(currentUserIdInt);
            LoggingConfigService.ReturnInfo($"Friend count: {friends.Count}");
            foreach (var f in friends)
                LoggingConfigService.ReturnInfo($"{f.Id} - {f.Email}");

            model.Friends = friends;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToFriend(int jobId, int friendId)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier).ToString();
            if (string.IsNullOrEmpty(senderId))
            {
                TempData["Error"] = "You must be logged in to send a job to a friend.";
                return RedirectToAction("Login", "Account");
            }
            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null) return NotFound();

            var jobUrl = Url.Action("Details", "Jobs", new { id = jobId }, Request.Scheme);
            var messageText = $"Check out this job: <a href='{jobUrl}'>{job.Title}</a>";
            // change senderId to int
            var senderIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(senderIdString, out int senderIdInt))
            {
                TempData["Error"] = "Invalid sender ID.";
                return RedirectToAction("Details", new { id = jobId });
            }

            var message = new Message
            {
                SenderId = senderIdInt,
                Sender = await _context.Users.FindAsync(senderIdInt),
                RecipientId = friendId,
                Recipient = await _context.Users.FindAsync(friendId),
                Content = messageText,
                SentAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Job sent to your friend's inbox!";
            return RedirectToAction("Details", new { id = jobId });
        }
    }
}
