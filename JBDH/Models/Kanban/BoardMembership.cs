using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.Kanban
{
    public class BoardMembership
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int BoardId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public string Role { get; set; } // Owner, Member, Viewer
        public int InvitedByUserId { get; set; }
        public bool InvitationAccepted { get; set; }
        public string InvitationToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BoardId")]
        public virtual Board Board { get; set; }
        // Optionally, add navigation to User if needed
    }
} 