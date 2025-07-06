using JBDH.Models.Job;
using JBDH.Models.User;
using System.Collections.Generic;

namespace JBDH.Models.ViewModels
{
    public class JobDetailsViewModel
    {
        public JBDH.Models.Job.Job? Job { get; set; }
        public int JobId { get; set; } // Keep JobId to manage actions like apply/unapply
        public bool HasApplied { get; set; } // To check if the user has applied
        public string Notes { get; set; } = string.Empty; // Notes the user might add

        // Add this property:
        public List<JBDH.Models.User.User>? Friends { get; set; } = new List<JBDH.Models.User.User>();
    }
}