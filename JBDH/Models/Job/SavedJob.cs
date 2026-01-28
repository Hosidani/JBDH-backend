using JBDH.Models.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.Job
{
    public class SavedJob
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int JobId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public User.User User { get; set; } = null!;

        [ForeignKey(nameof(JobId))]
        public Job Job { get; set; } = null!;
    }
}