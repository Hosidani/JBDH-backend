using System.Threading.Tasks;

namespace JBDH.Services.AICommunications
{
    public interface IJobAIAnalysisService
    {
        Task<string> GetOrCreateAnalysisAsync(string jobAdUrl, string jobId);
    }
}