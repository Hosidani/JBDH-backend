namespace JBDH.Models.AI
{
    public class Requirement
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Importance { get; set; }
    }

    public class Profession
    {
        public string Main { get; set; } = string.Empty;
        public string Sub { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
    }

    public class RequirementExtractionResponse
    {
        public List<Requirement> Requirements { get; set; } = new List<Requirement>();
        public Profession Profession { get; set; } = new Profession();
        public string? CareerSummaryPitch { get; set; }
    }
}
