using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;


namespace JBDH.Controllers
{

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    
    public HomeController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult SearchJobs(string description, string location)
    {
        var searchJobsService = new SearchJobsService(_context, _environment);
        if (string.IsNullOrEmpty(description) || string.IsNullOrEmpty(location))
        {
            LoggingConfigService.ReturnError("Description or location is empty.");
            return View("Error", new { Message = "Description and location cannot be empty." });
        }
        var jobs = searchJobsService.SearchJobs(description, location);
        return View("SearchResults",jobs);
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
}