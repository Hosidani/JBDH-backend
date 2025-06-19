using JBDH.Models.Job;
using JBDH.Models.Dataflow;
using FriendshipModel = JBDH.Models.Friendship.Friendship;
using System.ComponentModel.DataAnnotations;

namespace JBDH.Models.User
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }
        
        [Required]
        public required string Email { get; set; }
        
        public required string Location { get; set; }

        public string? CVPath { get; set; } // Path to uploaded CV
        public bool? DeveloperMode { get; set; } // Developer mode
        public bool? OpenForHiring { get; set; } // Active mode
        public bool IsBanned { get; set; } = false;

        public ICollection<FriendshipModel> SentFriendRequests { get; set; } = new List<FriendshipModel>();
        public ICollection<FriendshipModel> ReceivedFriendRequests { get; set; } = new List<FriendshipModel>();
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public ICollection<DataFlow> DataFlows { get; set; } = new List<DataFlow>();
        public ICollection<UserAIResponse> AIResponses { get; set; } = new List<UserAIResponse>();
        public ICollection<UserRoleAssignment> Roles { get; set; } = new List<UserRoleAssignment>();
        public ICollection<Organisation> ManagedOrganisations { get; set; } = new List<Organisation>();
        public ICollection<User> OwnedUsers { get; set; } = new List<User>();
        public User? Owner { get; set; }
        public int? OwnerId { get; set; }
    }
}
