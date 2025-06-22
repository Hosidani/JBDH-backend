using JBDH.Models.ViewModels;
using JBDH.Services;
using JBDH.Services.JobManagement;
using JBDH.Models.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JBDH.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly JobService _jobService;
        public JobsController(ApplicationDbContext context, JobService jobService)
        {
            _context = context;
            _jobService = jobService;
        }

        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
                return NotFound();

            // Get current user ID from claims
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId = 0;
            if (!string.IsNullOrEmpty(userIdStr))
                int.TryParse(userIdStr, out userId);

            bool hasApplied = userId > 0 &&
            await _context.JobApplications.AnyAsync(a => a.JobId == id && a.UserId == userId);

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

            return View(model);
        }
    }
}
