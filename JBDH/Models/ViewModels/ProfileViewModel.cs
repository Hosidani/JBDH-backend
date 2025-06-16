using JBDH.Services;
using JBDH.Models.User;

namespace JBDH.Models
{
    public class ProfileViewModel
    {
        public required JBDH.Models.User.User User { get; set; }
        public bool IsFriend { get; set; }
        public Organisation? Organisation { get; set; }
        public UserAIResponse? UserAIResponse { get; set; }
        public CVAnalysisResult? CVAnalysisResult { get; set; }

    }
}
