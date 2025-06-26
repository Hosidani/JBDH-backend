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

        public async Task<IActionResult> Details(string id)
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
                    .AnyAsync(a => a.JobId.ToString() == id && a.UserId.ToString() == userId);
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

            return View(model);
        }
    }
}
