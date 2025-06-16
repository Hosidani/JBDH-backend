using JBDH.Services;

namespace JBDH.Models
{
    public class ProfileViewModel
    {
        public required User User { get; set; }
        public bool IsFriend { get; set; }
        public Organisation? Organisation { get; set; } 
        public UserAIResponse? UserAIResponse { get; set; }

    }
}
