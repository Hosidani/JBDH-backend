namespace JBDH.Models
{
    public class UserRoleAssignment
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public string Role { get; set; } // "User", "Manager", "Recruiter"
    }
}