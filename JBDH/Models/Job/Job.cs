using JBDH.Models.User;
using System.ComponentModel.DataAnnotations;

namespace JBDH.Models.Job
{
    public class Job
    {
        [Key]
        public int JobId { get; set; }
        
        [MaxLength(200)]
        public string? Title { get; set; }
        
        [MaxLength(5000)]
        public string? Description { get; set; }
        
        [MaxLength(200)]
        public string? Company { get; set; }
        
        [MaxLength(200)]
        public string? Location { get; set; }
        
        public DateTime? PostedDate { get; set; }
        
        [MaxLength(500)]
        public string? WebpageUrl { get; set; }
        
        // NYA FÄLT (från steg 1)
        [MaxLength(50)]
        public string? WorkType { get; set; }
        
        [MaxLength(50)]
        public string? EmploymentType { get; set; }
        
        [MaxLength(100)]
        public string? ExternalJobId { get; set; }
        
        public DateTime? ApplicationDeadline { get; set; }
        
        [MaxLength(1000)]
        public string? RequiredSkills { get; set; }
        
        // Navigation - utan AutoInclude
        public int? RecruiterId { get; set; }
        public JBDH.Models.User.User? Recruiter { get; set; }
        
        public int? OwnerId { get; set; }
        public JBDH.Models.User.User? Owner { get; set; }
    }

}