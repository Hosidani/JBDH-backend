using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.ViewModels;

public interface IJobAIResponseService
{
    Task AttachLatestAIResponsesAsync(List<JobSearchResultViewModel> jobs);
}