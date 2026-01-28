using System.ComponentModel.DataAnnotations;

namespace JBDH.Models.Job
{
    public class NotInterestedJob
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required] 
        public int JobId { get; set; }
        
        public DateTime MarkedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User.User? User { get; set; }
        public Job? Job { get; set; }
    }
}