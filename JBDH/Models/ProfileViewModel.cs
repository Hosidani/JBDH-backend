using JBDH.Services;

namespace JBDH.Models
{
    public class ProfileViewModel
    {
        public User User { get; set; }
        public bool IsFriend { get; set; }
        public string Organization { get; set; } // Display organization only if verified
        public UserAIResponse UserAIResponse { get; set; } 

    }
}
