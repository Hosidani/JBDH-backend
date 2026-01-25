using JBDH.Models;
using JBDH.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using JBDH.Services.JobManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Workflows;
using JBDH.Models.Core;
using Newtonsoft.Json;
using JBDH.Services.Helpers;
using JBDH.Models.User;

namespace JBDH.Services.Workflows
{
    public class SearchWorkflowService : ISearchWorkflowService
    {
        private readonly ISearchJobsService _searchJobsService;
        private readonly IJobAIResponseService _jobAIResponseService;
        private readonly ApplicationDbContext _context;
        private readonly IMatchingAlgorithmCVJobService _matchingAlgorithmCVJobService;

        public SearchWorkflowService(
            ISearchJobsService searchJobsService,
            IJobAIResponseService jobAIResponseService,
            ApplicationDbContext context,
            IMatchingAlgorithmCVJobService matchingAlgorithmCVJobService 
        )
        {
            _searchJobsService = searchJobsService;
            _jobAIResponseService = jobAIResponseService;
            _context = context;
            _matchingAlgorithmCVJobService = matchingAlgorithmCVJobService;
        }

        public async Task<List<JobSearchResultViewModel>> SearchAndAttachMetaAsync(string description, string location, int? userId)
        {
            var results = await _searchJobsService.SearchJobsAsync(description, location);
            await _jobAIResponseService.AttachLatestAIResponsesAsync(results);

            CVAnalysisResult cvAI = null;
            if (userId.HasValue)
            {
                var jobIds = results.Where(r => r.Job != null).Select(r => r.Job.JobId).ToList();
                var appliedJobIds = await _context.JobApplications
                    .Where(a => jobIds.Contains(a.JobId) && a.UserId == userId.Value)
                    .Select(a => a.JobId)
                    .ToListAsync();

                foreach (var item in results)
                {
                    item.HasApplied = item.Job != null && appliedJobIds.Contains(item.Job.JobId);
                }

                // Get user's AI response for CV
                var userAIResponse = await _context.UserAIResponses
                    .Where(r => r.UserId == userId.Value)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                if (userAIResponse != null)
                    cvAI = JsonConvert.DeserializeObject<CVAnalysisResult>(userAIResponse.ResponseText);
            }

            foreach (var item in results)
            {
                if (item.LatestAIResponse != null && cvAI != null)
                {
                    var jobAI = JsonConvert.DeserializeObject<JobAdAIResponseViewModel>(item.LatestAIResponse.ResponseText);
                    item.CVMatchPercent = jobAI != null
                        ? _matchingAlgorithmCVJobService.CalculateJobTitleMatchPercentage(jobAI, cvAI)
                        : (int?)null;
                }
                else
                {
                    item.CVMatchPercent = null;
                }
            }

            return results;
        }
    }
}