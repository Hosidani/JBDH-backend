namespace JBDH.Models.ViewModels
{
    public class JobDetailsViewModel
    {
        public Job Job { get; set; }
         public int JobId { get; set; } // Keep JobId to manage actions like apply/unapply
        public bool HasApplied { get; set; } // To check if the user has applied
        public string Notes { get; set; } // Notes the user might add
    }
}