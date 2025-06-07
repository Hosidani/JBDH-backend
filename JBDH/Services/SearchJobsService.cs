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
                        JobId = dict.ContainsKey("JobId") ? dict["JobId"].ToString() : "0",
                        Title = dict.ContainsKey("Title") ? dict["Title"].ToString() : "",
                        Description = dict.ContainsKey("Description") ? dict["Description"].ToString() : "",
                        Company = dict.ContainsKey("Company") ? dict["Company"].ToString() : "",
                        Location = dict.ContainsKey("Location") ? dict["Location"].ToString() : "",
                        PostedDate = dict.ContainsKey("PostedDate") ? (DateTime?)Convert.ToDateTime(dict["PostedDate"]) : null,
                        WebpageUrl = dict.ContainsKey("WebpageUrl") ? dict["WebpageUrl"].ToString() : ""
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