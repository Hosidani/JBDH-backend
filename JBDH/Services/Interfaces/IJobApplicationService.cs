using System.Threading.Tasks;
using JBDH.Models;

public interface IJobApplicationService
{
    Task<bool> HasUserAppliedAsync(int userId, int jobId);
    Task<ServiceResult> ApplyJobAsync(int userId, int jobId, string notes);
    Task<ServiceResult> UnapplyJobAsync(int userId, int jobId);
}