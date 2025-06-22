// filepath: c:\Users\danie\Documents\JBDH_V0.1\JBDH\Services\AICommunications/JobAIResponseService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.ViewModels;
using JBDH.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services.AICommunications
{
    public class JobAIResponseService : IJobAIResponseService
    {
        private readonly ApplicationDbContext _context;
        public JobAIResponseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AttachLatestAIResponsesAsync(List<JobSearchResultViewModel> jobs)
        {
            foreach (var item in jobs)
            {
                if (item.Job != null && item.Job.JobId != null)
                {
                    item.LatestAIResponse = await _context.JobAIResponses
                        .Where(r => r.JobId == item.Job.JobId.ToString())
                        .OrderByDescending(r => r.CreatedAt)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    item.LatestAIResponse = null;
                }
            }
        }
    }
}