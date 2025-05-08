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
        public string Role { get; set; } // e.g., "User", "Manager", "Developer"

        [Required]
        public string Email { get; set; }

        public string CVPath { get; set; } // Path to uploaded CV
        public bool? DeveloperMode { get; set; } // Developer mode
        public bool? OpenForHiring { get; set; } // Active mode
        public bool IsOrganizationVerified { get; set; } // Track if the organization is verified
        public bool IsEmailConfirmed { get; set; } = false; // Indicates if the email is confirmed

        public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
        public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
    }
}
