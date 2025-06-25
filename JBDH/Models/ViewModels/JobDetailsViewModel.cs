using JBDH.Models.Job;

namespace JBDH.Models.ViewModels
{
    public class JobDetailsViewModel
    {
        public JBDH.Models.Job.Job? Job { get; set; }
        public string JobId { get; set; } // Keep JobId to manage actions like apply/unapply
        public bool HasApplied { get; set; } // To check if the user has applied
        public string Notes { get; set; } = string.Empty; // Notes the user might add
    }
}