using JBDH.Models;
using JBDH.Services.Workflows.Interfaces;
using JBDH.Models.ViewModels;
using Microsoft.EntityFrameworkCore;


public class SearchWorkflowService : ISearchWorkflowService
{
    private readonly ISearchJobsService _searchJobsService;
    private readonly IJobAIResponseService _jobAIResponseService;
    private readonly ApplicationDbContext _context;

    public SearchWorkflowService(
        ISearchJobsService searchJobsService,
        IJobAIResponseService jobAIResponseService,
        ApplicationDbContext context)
    {
        _searchJobsService = searchJobsService;
        _jobAIResponseService = jobAIResponseService;
        _context = context;
    }

    public async Task<List<JobSearchResultViewModel>> SearchAndAttachMetaAsync(string description, string location, int? userId)
    {
        var results = await _searchJobsService.SearchJobsAsync(description, location);
        await _jobAIResponseService.AttachLatestAIResponsesAsync(results);

        if (userId.HasValue)
        {
            foreach (var item in results)
            {
                item.HasApplied = await _context.JobApplications
                    .AnyAsync(a => a.JobId.ToString() == item.Job.JobId && a.UserId == userId.Value);
            }
        }

        return results;
    }
}