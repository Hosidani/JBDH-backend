using System;
using System.Collections.Generic;

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
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? AboutMe { get; set; }
        // Add other user fields as needed
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
