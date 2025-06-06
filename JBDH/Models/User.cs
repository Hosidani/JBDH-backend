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
        public string Email { get; set; }
        public string Location { get; set; }

        public string CVPath { get; set; } // Path to uploaded CV
        public bool? DeveloperMode { get; set; } // Developer mode
        public bool? OpenForHiring { get; set; } // Active mode
        public bool IsBanned { get; set; } = false;

        public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
        public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public ICollection<DataFlow> DataFlows { get; set; } = new List<DataFlow>();
        public ICollection<UserAIResponse> AIResponses { get; set; } = new List<UserAIResponse>();
        public ICollection<UserRoleAssignment> Roles { get; set; } = new List<UserRoleAssignment>();
        public int? OwnedByRecruiterId { get; set; }
        public User OwnedByRecruiter { get; set; }
        public ICollection<User> OwnedUsers { get; set; } = new List<User>();
    }
}
