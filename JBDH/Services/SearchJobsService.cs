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

        public List<JobSearchResultViewModel> SearchJobs(string description, string location)
        {
            LoggingConfigService.ReturnInfo("SearchJobs Service called");
            try
            {
                var jobDictionaries = InternalSortingService.InternalSortingFunction(description, location);
                LoggingConfigService.ReturnInfo($"SearchJobs method called with description: {description} and location: {location}");
                LoggingConfigService.ReturnInfo($"Results count: {jobDictionaries.Count}");

                // Map dictionaries to Job objects
                var jobs = new List<Job>();
                foreach (var dict in jobDictionaries)
                {
                    LoggingConfigService.ReturnInfo("Job dictionary keys: " + string.Join(", ", dict.Keys));
                    jobs.Add(new Job
                    {
                        JobId = dict.ContainsKey("job_id") ? dict["job_id"].ToString() : "0", // <-- assign as string
                        Title = dict["title"].ToString(),
                        Description = dict["description"].ToString(),
                        Company = dict["company"].ToString(),
                        Location = dict["location"].ToString(),
                        PostedDate = dict.ContainsKey("posted_date") ? (DateTime?)Convert.ToDateTime(dict["posted_date"]) : null,
                        WebpageUrl = dict["webpage_url"].ToString()
                    });
                }

                // Now assign priorities and return the view models
                var presentJobs = GiveJobPriorityService.AssignJobPriority(jobs, description, location);
                return presentJobs;
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error in SearchJobs: {ex.Message}");
                throw;
            }
        }
    }
}