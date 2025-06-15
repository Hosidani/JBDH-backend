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
        private readonly JobService _jobService;
        public JobsController(ApplicationDbContext context, JobService jobService)
        {
            _context = context;
            _jobService = jobService;
        }

        public async Task<IActionResult> Details(int id)
        {
            var username = User?.Identity?.Name;
            var viewModel = await _jobService.GetJobDetailsAsync(id, username);
            if (viewModel == null)
                return NotFound();
            return View(viewModel);
        }
    }
}
