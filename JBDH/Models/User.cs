using System.ComponentModel.DataAnnotations;

namespace JBDH.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // e.g., "User", "Manager", "Developer"

        [Required]
        public string Email { get; set; }

        public string CVPath { get; set; } // Path to uploaded CV
    }
}
