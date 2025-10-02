using JBDH.Models.ViewModels;
using JBDH.Models.User; // <-- Add this

namespace JBDH.Services.Helpers
{
    public interface IMatchingAlgorithmCVJobService
    {
        int CalculateJobTitleMatchPercentage(JobAdAIResponseViewModel jobAI, CVAnalysisResult cvAI);
    }
}


