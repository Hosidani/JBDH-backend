using System.Threading.Tasks;

namespace JBDH.Services.AICommunications.Interfaces
{
    public interface IJobAIAnalysisService
    {
        Task<string> GetOrCreateAnalysisAsync(string jobAdUrl, string jobId);
    }
}