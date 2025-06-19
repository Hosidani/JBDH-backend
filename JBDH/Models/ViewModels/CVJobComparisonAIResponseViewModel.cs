namespace JBDH.Models.ViewModels
{
    public class CVJobComparisonAIResponseViewModel
    {
        public string FitSummary { get; set; } = string.Empty;
        public List<string> MissingRequirements { get; set; } = new List<string>();
        public List<string> MatchingSkills { get; set; } = new List<string>();
        public List<string> RelevantExperiences { get; set; } = new List<string>();
        public List<string> EducationAndCertifications { get; set; } = new List<string>();
    }
}