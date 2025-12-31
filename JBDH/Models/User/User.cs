using JBDH.Models.Job;
using JBDH.Models.Dataflow;
using FriendshipModel = JBDH.Models.Friendship.Friendship;
using System.ComponentModel.DataAnnotations;
using JBDH.Models.AI;
using Organisation = JBDH.Models.Organisation;

namespace JBDH.Models.User
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string PasswordHash { get; set; }
        
        [Required]
        public required string Email { get; set; }
        
        public required string Location { get; set; }
        
        [Required]
        public string Role { get; set; } = "User";

        public string? Name { get; set; }
        // Backing field for EF Core storage
        public string? DesiredEmploymentTypesData { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<string>? DesiredEmploymentTypes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DesiredEmploymentTypesData)) return null;
                try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(DesiredEmploymentTypesData); }
                catch { return null; }
            }
            set
            {
                DesiredEmploymentTypesData = value == null ? null : System.Text.Json.JsonSerializer.Serialize(value);
            }
        }
        // Backing field for EF Core storage
        public string? DesiredWorkModesData { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<string>? DesiredWorkModes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DesiredWorkModesData)) return null;
                try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(DesiredWorkModesData); }
                catch { return null; }
            }
            set
            {
                DesiredWorkModesData = value == null ? null : System.Text.Json.JsonSerializer.Serialize(value);
            }
        }
        public string? PhoneNumber { get; set; }
        public bool TravelAvailability { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public string? DesiredCity { get; set; }
        public string? WorkPlace { get; set; }
        public string? Competencies { get; set; }
        public string? CVPath { get; set; } // Path to uploaded CV
        public bool? DeveloperMode { get; set; } // Developer mode
        public bool? Searchable { get; set; } // Active mode
        public bool IsBanned { get; set; } = false;

        public ICollection<FriendshipModel> SentFriendRequests { get; set; } = new List<FriendshipModel>();
        public ICollection<FriendshipModel> ReceivedFriendRequests { get; set; } = new List<FriendshipModel>();
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public ICollection<DataFlow> DataFlows { get; set; } = new List<DataFlow>();
        public ICollection<UserAIResponse> AIResponses { get; set; } = new List<UserAIResponse>();
        public ICollection<UserRoleAssignment> Roles { get; set; } = new List<UserRoleAssignment>();
        public ICollection<JBDH.Models.Organisation.Organisation> ManagedOrganisations { get; set; } = new List<JBDH.Models.Organisation.Organisation>();
        public ICollection<User> OwnedUsers { get; set; } = new List<User>();
        public User? Owner { get; set; }
        public int? OwnerId { get; set; }
        public string? AboutMe { get; set; }

    }
}
