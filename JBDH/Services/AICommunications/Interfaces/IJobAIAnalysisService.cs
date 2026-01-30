using System.Threading.Tasks;
using JBDH.Models.AI;

namespace JBDH.Services.AICommunications
{
    public interface IJobAIAnalysisService
    {
        Task<string> GetOrCreateAnalysisAsync(string jobAdUrl, int jobId);
        Task<string> GetCvJobComparisonAsync(string jobAdText, string cvPath);
        Task<(List<Requirement> requirements, Profession? jobProfession)> ExtractJobRequirementsAndProfessionAsync(string jobAdText);
        
        // Add the bulk processing methods
        Task<List<(int JobId, string WebpageUrl)>> GetAllJobURLsFromDatabaseAsync();
        Task ProcessAllJobsWithAIAsync(int batchSize = 10, int delayBetweenBatches = 2000, CancellationToken cancellationToken = default);
    }
}