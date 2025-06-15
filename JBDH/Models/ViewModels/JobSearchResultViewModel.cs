using JBDH.Models;

public class JobSearchResultViewModel
{
    public Job? Job { get; set; }
    public JobAIResponse? LatestAIResponse { get; set; }
    public int Priority { get; set; }
}