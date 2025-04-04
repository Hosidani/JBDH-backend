using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace JBDH.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult SearchJobs(string description, string location)
    {
        // Call the SearchApi to filter jobs
        var jobAds = SearchApi.FetchJobAdsAsync("https://jobstream.api.jobtechdev.se").Result;
        var filteredJobs = SearchApi.FilterJobs(jobAds, description, location);

        // Pass the filtered jobs to the view
        return View("SearchResults", filteredJobs);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Query the database for the user
        var user = _context.Users
            .FirstOrDefault(u => u.Username == username && u.PasswordHash == password);

        if (user != null)
        {
            // Set session or authentication cookie
            HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
            return RedirectToAction("Index", "Home");
        }

        // Return an error message if login fails
        ViewData["ErrorMessage"] = "Invalid username or password.";
        return View();
    }

    public IActionResult Settings()
    {
        var isLoggedIn = HttpContext.Session.GetString("IsLoggedIn");
        if (string.IsNullOrEmpty(isLoggedIn) || isLoggedIn != "true")
        {
            return RedirectToAction("Login"); // Redirect to login if not logged in
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
