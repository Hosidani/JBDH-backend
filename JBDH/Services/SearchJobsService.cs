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
            LoggingConfigService.ReturnInfo("SearchJobs Service called");
            try
            {
                var results = InternalSortingService.InternalSortingFunction(description, location);
                LoggingConfigService.ReturnInfo($"SearchJobs method called with description: {description} and location: {location}");
                LoggingConfigService.ReturnInfo($"Results count: {results.Count}");
                var presentJobs = GiveJobPriorityService.AssignJobPriority(results, description, location);
                return presentJobs.OrderBy(j => j.Priority).ToList();
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error in SearchJobs: {ex.Message}");
                throw;
            }
        }
    }
}