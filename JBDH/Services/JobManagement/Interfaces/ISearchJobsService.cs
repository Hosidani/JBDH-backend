using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.ViewModels;

namespace JBDH.Services.JobManagement
{
    public interface ISearchJobsService
    {
        Task<List<JobSearchResultViewModel>> SearchJobsAsync(string description, string location);
    }
}