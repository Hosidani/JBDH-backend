
public interface ISearchCandidateService
{
    Task<List<SearchCandidateViewModel>> SearchCandidates(string keyword, string location);
}