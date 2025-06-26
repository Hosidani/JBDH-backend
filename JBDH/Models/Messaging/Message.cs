using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JBDH.Models.User;

namespace JBDH.Models.Messaging
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public required JBDH.Models.User.User Sender { get; set; }

        [Required]
        public int RecipientId { get; set; }
        [ForeignKey(nameof(RecipientId))]
        public required JBDH.Models.User.User Recipient { get; set; }

        [Required]
        [StringLength(2000)]
        public required string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}