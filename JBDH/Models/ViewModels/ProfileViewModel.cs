using JBDH.Models.User;
using JBDH.Models.AI;
using JBDH.Models.Services;
using JBDH.Models.Job;


namespace JBDH.Models.ViewModels
{
    public class ProfileViewModel
    {
        public required JBDH.Models.User.User User { get; set; }
        public bool IsFriend { get; set; }
        public JBDH.Models.Organisation.Organisation? Organisation { get; set; }
        public UserAIResponse? UserAIResponse { get; set; }
        public CVAnalysisResult? CVAnalysisResult { get; set; }
        public List<JBDH.Models.User.User> Friends { get; set; } = new List<JBDH.Models.User.User>();
        public List<JBDH.Models.Friendship.Friendship> PendingFriendRequests { get; set; } = new();
        public List<JobApplicationViewModel> JobApplications { get; set; } = new();
        public AIUserCourseProposal CourseProposals { get; set; } // or string if you want to show raw JSON


        public class JobApplicationViewModel
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
}
