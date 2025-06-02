namespace JBDH.Models
{
    public class Job
    {
        public string JobId { get; set; } // Unique identifier for the job
        public string Title { get; set; } // Job title
        public string Description { get; set; } // Job description
        public string Company { get; set; } // Company offering the job
        public string Location { get; set; } // Job location
        public DateTime? PostedDate { get; set; } // Date the job was posted
        public string WebpageUrl { get; set; } // URL to the job posting
    }
}