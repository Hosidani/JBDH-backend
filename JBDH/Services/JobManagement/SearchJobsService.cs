using System.Linq;
using System.ComponentModel.DataAnnotations;
using JBDH.Models.Job;
using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using JBDH.Services.Configuration;
using JBDH.Services.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JBDH.Services.JobManagement
{
    public class SearchJobsService : ISearchJobsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SearchJobsService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<List<JobSearchResultViewModel>> SearchJobsAsync(string description, string location)
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
                        JobId = dict.TryGetValue("JobId", out var jobId) && jobId != null ? jobId.ToString()! : "0",
                        Title = dict.TryGetValue("Title", out var title) && title != null ? title.ToString()! : "",
                        Description = dict.TryGetValue("Description", out var desc) && desc != null ? desc.ToString()! : "",
                        Company = dict.TryGetValue("Company", out var company) && company != null ? company.ToString()! : "",
                        Location = dict.TryGetValue("Location", out var locValue) && locValue != null ? locValue.ToString()! : "",
                        PostedDate = dict.TryGetValue("PostedDate", out var postedDate) && postedDate != null
                            ? (DateTime?)Convert.ToDateTime(postedDate)
                            : null,
                        WebpageUrl = dict.TryGetValue("WebpageUrl", out var url) && url != null ? url.ToString()! : ""
                    });
                }

                // Await the async priority assignment
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