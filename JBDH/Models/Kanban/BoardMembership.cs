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
        public required string Role { get; set; } // Owner, Member, Viewer

        public int InvitedByUserId { get; set; }
        public bool InvitationAccepted { get; set; }
        public required string InvitationToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BoardId")]
        public required virtual Board Board { get; set; }
        // Optionally, add navigation to User if needed
    }
}