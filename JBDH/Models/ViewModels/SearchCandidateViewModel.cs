using JBDH.Models.AI;

namespace JBDH.Models.ViewModels
{
    public class SearchCandidateViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public UserAIResponse? UserAIResponse { get; set; }
        public int Priority { get; set; }
    }
}