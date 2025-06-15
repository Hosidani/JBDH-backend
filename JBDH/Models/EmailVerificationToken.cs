using System;
using System.ComponentModel.DataAnnotations;

namespace JBDH.Models
{
    public class EmailVerificationToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Token { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        public bool IsUsed { get; set; }
    }
}