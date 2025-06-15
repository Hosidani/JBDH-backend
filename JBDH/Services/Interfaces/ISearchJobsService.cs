using System.Collections.Generic;
using System.Threading.Tasks;
using JBDH.Models.ViewModels;


public interface ISearchJobsService
{
    Task<List<JobSearchResultViewModel>> SearchJobsAsync(string description, string location);
}