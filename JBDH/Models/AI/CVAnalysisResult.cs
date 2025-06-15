namespace JBDH.Models.AI
{
    public class CVAnalysisResult
    {
        public string BestFitJobTitle { get; set; }
        public int MatchScore { get; set; }
        public string? Reasoning { get; set; }
        public List<string>? RecommendedJobs { get; set; }
    }
}