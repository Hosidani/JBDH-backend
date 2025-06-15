using JBDH.Models;
using JBDH.Models.ViewModels;

namespace JBDH.Services.Interfaces
{
    public interface IJobService
    {
        Task<JobDetailsViewModel?> GetJobDetailsAsync(int jobId, string? username);
    }
}