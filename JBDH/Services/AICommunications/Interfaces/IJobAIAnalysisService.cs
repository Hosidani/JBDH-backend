using System.Threading.Tasks;
using JBDH.Models.AI;

namespace JBDH.Services.AICommunications
{
    public interface IJobAIAnalysisService
    {
        Task<string> GetOrCreateAnalysisAsync(string jobAdUrl, int jobId);
        Task<string> GetCvJobComparisonAsync(string jobAdText, string cvPath);
        Task<(List<Requirement> requirements, Profession? jobProfession)> ExtractJobRequirementsAndProfessionAsync(string jobAdText);
    }
}