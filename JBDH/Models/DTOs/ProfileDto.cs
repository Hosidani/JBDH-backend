using System;
using System.Collections.Generic;
using FriendshipModel = JBDH.Models.Friendship.Friendship;
using JBDH.Models.Job;
using JBDH.Models.Dataflow;
using JBDH.Models.AI;
using JBDH.Models.User;
using JBDH.Models.Organisation;

namespace JBDH.Models.DTOs
{
    public class ProfileDto
    {
        public UserDto User { get; set; } = new();
        public bool IsFriend { get; set; }
        public List<FriendDto> Friends { get; set; } = new();
        public List<FriendRequestDto> PendingFriendRequests { get; set; } = new();
        public List<JobApplicationDto> JobApplications { get; set; } = new();
        public object? CVAnalysis { get; set; }
        public string? CVAnalysisJson { get; set; }
        public object? CourseProposals { get; set; }
        public string? CourseProposalsJson { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string? Password { get; set; }
        public string? PasswordHash { get; set; }
        public string? Email { get; set; }
        public string? Location { get; set; }
        public string? Name { get; set; }
        public string? DesiredEmploymentType { get; set; }
        public string? DesiredWorkMode { get; set; }
        public string? PhoneNumber { get; set; }
        public bool TravelAvailability { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public string? DesiredCity { get; set; }
        public string? WorkPlace { get; set; }
        public string? Competencies { get; set; }
        public string? CVPath { get; set; }
        public bool? DeveloperMode { get; set; }
        public bool? Searchable { get; set; }
        public bool IsBanned { get; set; }
        public List<string>? SelectedRoles { get; set; }
        public List<FriendshipModel>? SentFriendRequests { get; set; }
        public List<FriendshipModel>? ReceivedFriendRequests { get; set; }
        public List<JobApplication>? JobApplications { get; set; }
        public List<DataFlow>? DataFlows { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public List<UserAIResponseDto>? AIResponses { get; set; }
        public List<UserRoleAssignment>? Roles { get; set; }
        public List<Organisation.Organisation>? ManagedOrganisations { get; set; }
        public List<UserDto>? OwnedUsers { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public UserDto? Owner { get; set; }
        public int? OwnerId { get; set; }
        public string? AboutMe { get; set; }

        
    }

    public class FriendDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
    }

    public class FriendRequestDto
    {
        public int Id { get; set; }
        public string? RequesterName { get; set; }
    }

    public class JobApplicationDto
    {
        public int ApplicationId { get; set; }
        public int JobId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string JobUrl { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime AppliedAt { get; set; }
    }
}
