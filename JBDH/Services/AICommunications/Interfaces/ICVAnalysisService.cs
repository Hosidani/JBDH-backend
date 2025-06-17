using JBDH.Models.User;
using System.Threading.Tasks;

public interface ICVAnalysisService
{
    Task AnalyzeAndSaveCVAsync(User user, string cvPath);
}