using JBDH.Models.User;
using System.ComponentModel.DataAnnotations;

namespace JBDH.Models.Job
{
    public class Job
    {
        [Key]
        public int JobId { get; set; } // Unique identifier for the job
        public string? Title { get; set; } // Job title
        public string? Description { get; set; } // Job description
        public string? Company { get; set; } // Company offering the job
        public string? Location { get; set; } // Job location
        public DateTime? PostedDate { get; set; } // Date the job was posted
        public string? WebpageUrl { get; set; } // URL to the job posting
        public int? RecruiterId { get; set; }
        public JBDH.Models.User.User? Recruiter { get; set; }
        public int? OwnerId { get; set; }
        public JBDH.Models.User.User? Owner { get; set; }
    }
}