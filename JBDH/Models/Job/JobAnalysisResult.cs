namespace JBDH.Models.Job
{
    public class JobAnalysisResult
    {
        public List<string> BestJobTitlesForUser { get; set; } = new List<string>();
        public List<string?> ReasoningBehindJobTitles { get; set; } = new List<string?>();
        public List<string> HardRequirements { get; set; } = new List<string>();
        public List<string> GoodToHaves { get; set; } = new List<string>();
        public List<string> Experiences { get; set; } = new List<string>();
        public List<string> EducationAndCertifications { get; set; } = new List<string>();
    }
}