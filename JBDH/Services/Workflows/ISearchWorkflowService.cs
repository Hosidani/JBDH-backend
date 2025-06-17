using JBDH.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JBDH.Services.Workflows.Interfaces
{
    public interface ISearchWorkflowService
    {
        Task<List<JobSearchResultViewModel>> SearchAndAttachMetaAsync(string description, string location, int? userId);
    }
}