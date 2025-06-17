using JBDH.Models.Job;
using JBDH.Models.AI;


public class JobSearchResultViewModel
{
    public Job Job { get; set; }
    public int Priority { get; set; }
    public bool HasApplied { get; set; }
    public JobAIResponse? LatestAIResponse { get; set; } // <-- Add this line
    // ... other properties ...
}