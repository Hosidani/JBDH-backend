using System.Collections.Generic;

public interface ISearchCandidateService
{
    List<SearchCandidateViewModel> SearchCandidates(string keyword, string location);
}