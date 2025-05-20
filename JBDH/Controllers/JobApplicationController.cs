using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Controllers
{
    public class JobApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJobApplicationService _jobAppService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JobApplicationsController(ApplicationDbContext context, IJobApplicationService jobAppService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _jobAppService = jobAppService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public async Task<IActionResult> Apply(int jobId, string notes)
        {
            LoggingConfigService.ReturnInfo($"Applying for job {jobId} with notes: {notes} IN CONTROLLER");
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            await _jobAppService.ApplyJobAsync(user.Id, jobId, notes);
            return RedirectToAction("Details", "Jobs", new { id = jobId });
        }

        [HttpPost]
        public async Task<IActionResult> Unapply(int jobId)
        {
            LoggingConfigService.ReturnInfo($"Unapplying from job {jobId} IN CONTROLLER");
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            await _jobAppService.UnapplyJobAsync(user.Id, jobId);
            return RedirectToAction("Details", "Jobs", new { id = jobId });
        }
    }
}