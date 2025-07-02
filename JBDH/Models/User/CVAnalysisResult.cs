using System.Text.Json.Serialization;

namespace JBDH.Models.User
{
    public class CVAnalysisResult
    {
        public List<string> BestJobTitlesForUser { get; set; } = new List<string>();
        public string? ReasoningBehindJobTitles { get; set; }
        public List<string> HardSkills { get; set; } = new List<string>();
        public List<string> SoftSkills { get; set; } = new List<string>();
        public List<ExperienceItem> Experiences { get; set; } = new List<ExperienceItem>();
        public List<string> EducationAndCertifications { get; set; } = new List<string>();
    }

    public class ExperienceItem
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("duration")]
        public required string Duration { get; set; }
    }
}