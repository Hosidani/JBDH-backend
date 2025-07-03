using JBDH.Models;
using JBDH.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using JBDH.Services.JobManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Workflows;
using JBDH.Models.Core;

namespace JBDH.Services.Workflows
{
    public class SearchWorkflowService : ISearchWorkflowService
    {
        private readonly ISearchJobsService _searchJobsService;
        private readonly IJobAIResponseService _jobAIResponseService;
        private readonly ApplicationDbContext _context;

        public SearchWorkflowService(
            ISearchJobsService searchJobsService,
            IJobAIResponseService jobAIResponseService,
            ApplicationDbContext context)
        {
            _searchJobsService = searchJobsService;
            _jobAIResponseService = jobAIResponseService;
            _context = context;
        }

        public async Task<List<JobSearchResultViewModel>> SearchAndAttachMetaAsync(string description, string location, int? userId)
        {
            var results = await _searchJobsService.SearchJobsAsync(description, location);
            await _jobAIResponseService.AttachLatestAIResponsesAsync(results);

            if (userId.HasValue)
            {
                foreach (var item in results)
                {
                    if (item.Job != null)
                    {
                        item.HasApplied = await _context.JobApplications
                            .AnyAsync(a => a.JobId == item.Job.JobId && a.UserId == userId.Value);
                    }
                    else
                    {
                        item.HasApplied = false;
                    }
                }
            }

            return results;
        }
    }
}