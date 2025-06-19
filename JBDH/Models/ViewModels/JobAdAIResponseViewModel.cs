public class JobAdAIResponseViewModel
{
    public string JobTitle { get; set; } = string.Empty;
    public List<string> HardRequirements { get; set; } = new List<string>();
    public List<string> GoodToHaves { get; set; } = new List<string>();
    public List<string> Experiences { get; set; } = new List<string>();
    public List<string> EducationAndCertifications { get; set; } = new List<string>();
    public string JobSummary { get; set; } = string.Empty;
}