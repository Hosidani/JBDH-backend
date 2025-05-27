using System.ComponentModel.DataAnnotations;

namespace JBDH.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // e.g., "User", "Manager", "Developer", "Recuriture", "Sales"

        [Required]
        public string Email { get; set; }

        public string CVPath { get; set; } // Path to uploaded CV
        public bool? DeveloperMode { get; set; } // Developer mode
        public bool? OpenForHiring { get; set; } // Active mode
        public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
        public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public ICollection<DataFlow> DataFlows { get; set; } = new List<DataFlow>();
    }
}
