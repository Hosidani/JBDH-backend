using JBDH.Models.User;

namespace JBDH.Models
{
    public class AIRequest
    {
        public JBDH.Models.User.User? User { get; set; }
        public int CommandId { get; set; }
        public string? CVPath { get; set; }
        public string? JobAdUrl { get; set; }
        public string? UserAIResponse { get; set; }
        public string? JobAIResponse { get; set; }
    }
}