using System.Threading.Tasks;

namespace JBDH.Services.AICommunications
{
    public interface IJobAIAnalysisService
    {
        Task<string> GetOrCreateAnalysisAsync(string jobAdUrl, int jobId);
        Task<string> GetCvJobComparisonAsync(string jobAdText, string cvPath);
    }
}