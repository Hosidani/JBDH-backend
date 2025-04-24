using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JBDH.Models;
using JBDH.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace JBDH.Services
{
    public class SearchJobsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SearchJobsService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public List<(int Priority, Dictionary<string, object> Job)> SearchJobs(string description, string location)
        {
            LoggingConfigService.ReturnInfo("SearchJobs method called");
            try
            {
                var results = InternalSortingService.InternalSortingFunction(description, location);
                foreach (var res in results)
                {
                    foreach (var kvp in res)
                    {
                        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    }
                }
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
                }
                return presentJobs.OrderByDescending(j => j.Priority).ToList();
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error in SearchJobs: {ex.Message}");
                throw;
            }
        }
    }
}