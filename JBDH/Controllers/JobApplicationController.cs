using JBDH.Services.JobManagement;
using JBDH.Services.Configuration;
using JBDH.Models.Core;
using JBDH.Models.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Controllers
{
    public class JobApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IJobApplicationService _jobAppService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JobApplicationController(ApplicationDbContext context, IJobApplicationService jobAppService, IHttpContextAccessor httpContextAccessor)
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
            if (user == null)
                return Unauthorized();
            var result = await _jobAppService.ApplyJobAsync(user.Id, jobId, notes);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("JobDetails", new { id = jobId });
            }
            var referer = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(referer))
                return RedirectToAction("Index", "Home");
            return Redirect(referer);
        }

        [HttpPost]
        public async Task<IActionResult> Unapply(int jobId)
        {
            LoggingConfigService.ReturnInfo($"Unapplying from job {jobId} IN CONTROLLER");
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            var result = await _jobAppService.UnapplyJobAsync(user.Id, jobId);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("JobDetails", new { id = jobId });
            }

            var referer = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(referer))
                return RedirectToAction("Index", "Home");
            return Redirect(referer);
        }
    }
}