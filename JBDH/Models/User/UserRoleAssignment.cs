namespace JBDH.Models
{
    public class UserRoleAssignment
    {
        public int UserId { get; set; }
        public required User User { get; set; }
        public required string Role { get; set; } // "User", "Manager", "Recruiter"
    }
}