namespace JBDH.Models.AI
{
    public class CVAnalysisResult
    {
        public List<string> BestJobTitlesForUser { get; set; } = new List<string>();
        public List<string?> ReasoningBehindJobTitles { get; set; } = new List<string?>();
        public List<string> HardSkills { get; set; } = new List<string>();
        public List<string> SoftSkills { get; set; } = new List<string>();
        public List<string> Experiences { get; set; } = new List<string>();
        public List<string> EducationAndCertifications { get; set; } = new List<string>();
    }
}