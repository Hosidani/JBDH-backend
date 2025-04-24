using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace JBDH.Controllers;

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
        LoggingConfigService.ReturnInfo("SearchJobs method called");
        try
        {
            var results = InternalSorting.InternalSortingFunction(description, location);
            LoggingConfigService.ReturnInfo($"SearchJobs method called with description: {description} and location: {location}");
            LoggingConfigService.ReturnInfo($"Results count: {results.Count}");
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
                        LoggingConfigService.ReturnInfo($"Assigned Priority 1: Title = {jobTitle}, Location = {jobLocation}");
                    }
                else if (jobTitle.Contains(description, StringComparison.OrdinalIgnoreCase))
                {
                    jobPriority = 2;
                    LoggingConfigService.ReturnInfo($"Assigned Priority 2: Title = {jobTitle}, Location = {jobLocation}");
                }
                else if (location == jobLocation)
                {
                    jobPriority = 3;
                    LoggingConfigService.ReturnInfo($"Assigned Priority 3: Title = {jobTitle}, Location = {jobLocation}");
                }

                // Log the assigned priority
                LoggingConfigService.ReturnInfo($"Assigned Priority {jobPriority}: Title = {jobTitle}, Location = {jobLocation}");

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
                LoggingConfigService.ReturnInfo($"Priority {priorityCount.Priority}: {priorityCount.Count} jobs");
            }
            presentJobs.Sort((x, y) => x.Priority.CompareTo(y.Priority));
            var sortedJobs = presentJobs.Select(x => x.Job).ToList();
            return View("SearchResults", sortedJobs);
        }
        catch (Exception ex)
        {
            LoggingConfigService.ReturnError($"Error in SearchJobs: {ex.Message}");
            return View("Error", new { Message = "An error occurred while searching for jobs." });
        }
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
