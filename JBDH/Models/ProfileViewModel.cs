using JBDH.Services;

namespace JBDH.Models
{
    public class ProfileViewModel
    {
        public User User { get; set; }
        public bool IsFriend { get; set; }
        public string Organisation { get; set; } 
        public UserAIResponse UserAIResponse { get; set; } 

    }
}
