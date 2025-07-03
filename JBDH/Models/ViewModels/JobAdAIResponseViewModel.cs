namespace JBDH.Models.ViewModels
{
    public class JobAdAIResponseViewModel
    {
        public List<string> JobTitle { get; set; } = new List<string>();
        public List<string> HardRequirements { get; set; } = new List<string>();
        public List<string> GoodToHaves { get; set; } = new List<string>();
        public List<string> Experiences { get; set; } = new List<string>();
        public List<string> EducationAndCertifications { get; set; } = new List<string>();
        public string JobSummary { get; set; } = string.Empty;
    }
}