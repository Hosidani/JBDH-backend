using JBDH.Models.Job;
using JBDH.Models.AI;
using JobModel = JBDH.Models.Job.Job;


namespace JBDH.Models.ViewModels
{
    public class JobSearchResultViewModel
    {
        public JobModel? Job { get; set; }
        public int Priority { get; set; }
        public bool HasApplied { get; set; }
        public JobAIResponse? LatestAIResponse { get; set; } // <-- Add this line
        // ... other properties ...
    }
}