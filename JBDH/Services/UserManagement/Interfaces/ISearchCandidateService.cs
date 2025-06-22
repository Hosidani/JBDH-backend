using JBDH.Models.ViewModels;

namespace JBDH.Services.UserManagement
{
    public interface ISearchCandidateService
    {
        Task<List<SearchCandidateViewModel>> SearchCandidates(string keyword, string location);
    }
}