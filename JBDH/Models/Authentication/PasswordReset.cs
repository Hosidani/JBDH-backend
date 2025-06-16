using JBDH.Models.User;

namespace JBDH.Models
{
    public class PasswordReset
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required JBDH.Models.User.User User { get; set; }
        public required string Token { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}