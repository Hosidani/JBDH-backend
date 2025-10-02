using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.ViewModels;
using JBDH.Models.AI; 

namespace JBDH.Services.AICommunications
{
    // Example interface
    public interface IJobAIResponseService
    {
        Task<JobAIResponse?> GetLatestJobAIResponseAsync(int jobId);
        Task AttachLatestAIResponsesAsync(List<JobSearchResultViewModel> jobs);
    }
}