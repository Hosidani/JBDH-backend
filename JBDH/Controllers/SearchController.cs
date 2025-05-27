using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;

namespace JBDH.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SearchController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult SearchForJobs(string description, string location)
        {
            var searchJobsService = new SearchJobsService(_context, _environment);

            if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(location))
            {
                LoggingConfigService.ReturnError("Description or location is empty.");
                return View("Error", new ErrorViewModel
                {
                    Title = "Missing Input",
                    Message = "Description and location cannot be empty.",
                    ReturnUrl = Url.Action("Index", "Home")
                });
            }

            var jobs = searchJobsService.SearchJobs(description, location);
            return View("SearchResults", jobs);
        }

        public IActionResult SearchJob()
        {
            return View();
        }

        public IActionResult Search()
        {
            return View();
        }

    }
}