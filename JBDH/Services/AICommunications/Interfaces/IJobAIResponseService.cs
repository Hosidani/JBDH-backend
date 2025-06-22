using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.ViewModels;

namespace JBDH.Services.AICommunications
{
    public interface IJobAIResponseService
    {
        Task AttachLatestAIResponsesAsync(List<JobSearchResultViewModel> jobs);
    }
}