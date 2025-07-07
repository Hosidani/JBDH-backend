using JBDH.Models.ViewModels;
using JBDH.Models.User;

namespace JBDH.Services.Helpers
{
    public class MatchingAlgorithmCVJobService : IMatchingAlgorithmCVJobService
    {
        public int CalculateJobTitleMatchPercentage(JobAdAIResponseViewModel jobAI, CVAnalysisResult cvAI)
        {
            if (jobAI?.JobTitle == null || cvAI?.BestJobTitlesForUser == null || jobAI.JobTitle.Count == 0 || cvAI.BestJobTitlesForUser.Count == 0)
                return 0;

            var jobTitles = jobAI.JobTitle.Select(j => j.ToLowerInvariant()).ToList();
            var cvTitles = cvAI.BestJobTitlesForUser.Select(c => c.ToLowerInvariant()).ToList();

            int matches = jobTitles.Intersect(cvTitles).Count();
            int total = jobTitles.Count;

            return total == 0 ? 0 : (int)Math.Round((matches / (double)total) * 100);
        }
    }
}