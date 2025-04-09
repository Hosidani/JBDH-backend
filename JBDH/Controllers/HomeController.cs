using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace JBDH.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _logger = logger;
        _context = context;
        _environment = environment;
    }

    public IActionResult Index()
    {
        ViewData["IsLoggedIn"] = HttpContext.Session.GetString("Username") != null;
        ViewData["Username"] = HttpContext.Session.GetString("Username");
        return View();
    }

    [HttpGet]
    public IActionResult SearchJobs(string description, string location)
    {
        Console.WriteLine("SearchJobs method called");
        try
        {
            var results = InternalSorting.InternalSortingFunction(description, location);
            Console.WriteLine($"Returning {results.Count} job results");

            var presentJobs = new List<(int Priority, Dictionary<string, object> Job)>();
            foreach (var job in results)
            {
                var jobTitle = job.GetValueOrDefault("title", "").ToString();
                var jobLocation = job.GetValueOrDefault("location", "").ToString();
                int jobPriority = 0;
                // Skip this job if title or location is invalid
                if (string.IsNullOrEmpty(jobTitle) || string.IsNullOrEmpty(jobLocation))
                {
                    continue;
                }
                if (jobTitle.Contains(description, StringComparison.OrdinalIgnoreCase) &&
                        jobLocation.Contains(location, StringComparison.OrdinalIgnoreCase))
                    {
                        jobPriority = 1;
                        Console.WriteLine($"Assigned Priority 1: Title = {jobTitle}, Location = {jobLocation}");
                    }
                else if (jobTitle.Contains(description, StringComparison.OrdinalIgnoreCase))
                {
                    jobPriority = 2;
                    Console.WriteLine($"Assigned Priority 2: Title = {jobTitle}, Location = {jobLocation}");
                }
                else if (location == jobLocation)
                {
                    jobPriority = 3;
                    Console.WriteLine($"Assigned Priority 3: Title = {jobTitle}, Location = {jobLocation}");
                }

                // Log the assigned priority
                Console.WriteLine($"Assigned Priority {jobPriority}: Title = {jobTitle}, Location = {jobLocation}");

                // Add the job to the list with its priority
                if ( jobPriority != 0 ){
                    presentJobs.Add((jobPriority, job));
                }
                
            }
            // Group jobs by priority and count the number of jobs in each priority group
            var priorityCounts = presentJobs
                .GroupBy(job => job.Priority)
                .Select(group => new { Priority = group.Key, Count = group.Count() })
                .ToList();

            // Print the counts for each priority
            foreach (var priorityCount in priorityCounts)
            {
                Console.WriteLine($"Priority {priorityCount.Priority}: {priorityCount.Count} jobs");
            }
            presentJobs.Sort((x, y) => x.Priority.CompareTo(y.Priority));
            var sortedJobs = presentJobs.Select(x => x.Job).ToList();
            return View("SearchResults", sortedJobs);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching or filtering jobs: {ex.Message}");
            return View("Error", new { Message = "An error occurred while searching for jobs." });
        }
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

    [HttpPost]
    public IActionResult UploadCV(IFormFile cvFile)
    {
        if (cvFile != null && cvFile.Length > 0)
        {
            try
            {
                // Save the CV file to the "wwwroot/uploads" directory
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var filePath = Path.Combine(uploadsPath, cvFile.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    cvFile.CopyTo(stream);
                }

                ViewData["Message"] = "CV uploaded successfully!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading CV: {ex.Message}");
                ViewData["ErrorMessage"] = "An error occurred while uploading your CV.";
            }
        }
        else
        {
            ViewData["ErrorMessage"] = "Please select a file to upload.";
        }

        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
