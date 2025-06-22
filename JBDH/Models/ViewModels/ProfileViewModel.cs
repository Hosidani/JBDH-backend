using JBDH.Models.User;
using JBDH.Models.AI;
using JBDH.Models.Services;


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
    }
}
