using System.Threading.Tasks;

public interface IJobApplicationService
{
    Task<bool> HasUserAppliedAsync(int userId, int jobId);
    Task<bool> ApplyJobAsync(int userId, int jobId, string notes);
    Task<bool> UnapplyJobAsync(int userId, int jobId);
}