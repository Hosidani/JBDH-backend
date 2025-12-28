using JBDH.Models.User;
using JBDH.Models.AI;
using System.Threading.Tasks;

namespace JBDH.Services.AICommunications
{
public interface ICVAnalysisService
    {
    Task AnalyzeAndSaveCVAsync(User user, string cvPath);
    Task<RequirementExtractionResponse?> ExtractCVProfessionAndRequirementsAsync(string cvText);
    }
}