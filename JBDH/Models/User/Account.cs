using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.User
{
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string AccountName { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public required string AccountEmail { get; set; }

        [MaxLength(20)]
        public string? AccountPhone { get; set; }

        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "en";

        public AccountType AccountType { get; set; } = AccountType.Basic;

        public bool IsTerminated { get; set; } = false;

        public DateTime? TerminatedAt { get; set; }

        public bool AcceptedTermsAndConditions { get; set; } = false;

        public DateTime? TermsAcceptedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public Account() 
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
}