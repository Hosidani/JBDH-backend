using JBDH.Models.ViewModels;
using JBDH.Models.User;

namespace JBDH.Services.Helpers
{
    public class MatchingAlgorithmCVJobService : IMatchingAlgorithmCVJobService
    {
        /*
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
        */
        public int CalculateJobTitleMatchPercentage(JobAdAIResponseViewModel jobAI, CVAnalysisResult cvAI)
        {
            if (jobAI == null || cvAI == null)
                return 0;

            // Job Titles
            var jobTitles = jobAI.JobTitle?.Select(j => j.ToLowerInvariant()).ToList() ?? new List<string>();
            var cvTitles = cvAI.BestJobTitlesForUser?.Select(c => c.ToLowerInvariant()).ToList() ?? new List<string>();
            int titleMatches = jobTitles.Count == 0 ? 0 : jobTitles.Count(jt => cvTitles.Any(ct => ct.Contains(jt) || jt.Contains(ct)));
            double titleScore = jobTitles.Count == 0 ? 0 : (titleMatches / (double)jobTitles.Count) * 100;

            // Hard Skills
            var jobSkills = jobAI.HardRequirements?.Select(s => s.ToLowerInvariant()).ToList() ?? new List<string>();
            var cvSkills = cvAI.HardSkills?.Select(s => s.ToLowerInvariant()).ToList() ?? new List<string>();
            int skillMatches = jobSkills.Count == 0 ? 0 : jobSkills.Count(js => cvSkills.Any(cs => cs.Contains(js) || js.Contains(cs)));
            double skillScore = jobSkills.Count == 0 ? 0 : (skillMatches / (double)jobSkills.Count) * 100;

            // Experiences
            var jobExps = jobAI.Experiences?.Select(e => e.ToLowerInvariant()).ToList() ?? new List<string>();
            var cvExps = cvAI.Experiences?.Select(e => e.Title.ToLowerInvariant()).ToList() ?? new List<string>();
            int expMatches = jobExps.Count == 0 ? 0 : jobExps.Count(je => cvExps.Any(ce => ce.Contains(je) || je.Contains(ce)));
            double expScore = jobExps.Count == 0 ? 0 : (expMatches / (double)jobExps.Count) * 100;

            // Education & Certifications
            var jobEdu = jobAI.EducationAndCertifications?.Select(e => e.ToLowerInvariant()).ToList() ?? new List<string>();
            var cvEdu = cvAI.EducationAndCertifications?.Select(e => e.ToLowerInvariant()).ToList() ?? new List<string>();
            int eduMatches = jobEdu.Count == 0 ? 0 : jobEdu.Count(je => cvEdu.Any(ce => ce.Contains(je) || je.Contains(ce)));
            double eduScore = jobEdu.Count == 0 ? 0 : (eduMatches / (double)jobEdu.Count) * 100;

            // Weights
            double finalScore =
                titleScore * 0.3 +
                skillScore * 0.4 +
                expScore * 0.2 +
                eduScore * 0.1;

            return (int)Math.Round(finalScore);
        }
    }
}