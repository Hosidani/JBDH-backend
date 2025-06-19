using JBDH.Models.User;
using System.Threading.Tasks;

namespace JBDH.Services.AICommunications
{
public interface ICVAnalysisService
    {
    Task AnalyzeAndSaveCVAsync(User user, string cvPath);
    }
}