using JBDH.Models;
using JBDH.Models.ViewModels;

namespace JBDH.Services.JobManagement
{
    public interface IJobService
    {
        Task<JobDetailsViewModel?> GetJobDetailsAsync(int jobId, string? username);
    }
}