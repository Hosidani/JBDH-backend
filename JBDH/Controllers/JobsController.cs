using JBDH.Models.ViewModels;
using JBDH.Services.JobManagement;
using JBDH.Models.Core;
using Microsoft.AspNetCore.Mvc;
using JBDH.Swagger;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JBDH.Models.Messaging;
using JBDH.Services.Friendship;
using JBDH.Services.Configuration;
using JBDH.Helpers;

namespace JBDH.Controllers
{
    [IncludeInApi]
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJobService _jobService;
        private readonly IFriendService _friendService;
        public JobsController(ApplicationDbContext context, IJobService jobService, IFriendService friendService)
        {
            _context = context;
            _jobService = jobService;
            _friendService = friendService;
        }

        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
                return this.ErrorPage("Job not found.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return this.ErrorPage("Invalid user ID.");

            bool hasApplied = await _context.JobApplications.AnyAsync(a => a.JobId == id && a.UserId == userId);
            var latestAIResponse = await _context.JobAIResponses
                .Where(r => r.JobId == id)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            var friends = await _friendService.GetFriendsAsync(userId);
            LoggingConfigService.ReturnInfo($"Friend count: {friends.Count}");
            foreach (var f in friends)
                LoggingConfigService.ReturnInfo($"{f.Id} - {f.Email}");

            var model = new JobDetailsViewModel
            {
                Job = job,
                JobId = id,
                HasApplied = hasApplied,
                Friends = friends
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToFriend(int jobId, int friendId)
        {
            var senderIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(senderIdStr, out int senderIdInt))
                return this.ErrorPage("You must be logged in to send a job to a friend.");

            var job = await _context.Jobs.FindAsync(jobId);
            if (job == null)
                return this.ErrorPage("Job not found.");

            var jobUrl = Url.Action("Details", "Jobs", new { id = jobId }, Request.Scheme);
            var messageText = $"Check out this job: <a href='{jobUrl}'>{job.Title}</a>";

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

            return this.SetSuccessAndRedirect("Job sent to your friend's inbox!", "Details", new { id = jobId });
        }
    }
}
