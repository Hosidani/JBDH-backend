using System.Threading.Tasks;

namespace JBDH.Services.Interfaces
{
    public interface IJobAIAnalysisService
    {
        Task<string> GetOrCreateAnalysisAsync(string jobAdUrl, string jobId);
    }
}