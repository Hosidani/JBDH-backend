using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JBDH.Models;
using JBDH.Models.ViewModels;
using JBDH.Services;

namespace JBDH.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Details(int id)
        {   
            LoggingConfigService.ReturnInfo($"Job ID: {id}"); // Log job ID for debugging
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
                return NotFound();
            LoggingConfigService.ReturnError($"Job: {job.Title}"); // Log job title for debugging
            // Replace with your own logic for getting the logged-in user
            var username = User?.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            LoggingConfigService.ReturnError($"User: {user?.Username}");
            LoggingConfigService.ReturnError($"Job: {job?.Title}"); 
            var hasApplied = user != null && await _context.JobApplications.AnyAsync(j => j.UserId == user.Id && j.JobId == id);
            var viewModel = new JobDetailsViewModel
            {
                Job = job,
                HasApplied = hasApplied
            };

            return View(viewModel);
        }
    }
}
