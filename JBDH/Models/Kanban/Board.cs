using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.Kanban
{
    public class Board
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }
        [Required]
        public int OwnerId { get; set; }
        public bool IsOrganizationBoard { get; set; }
        public int? OrganizationId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<KanbanList> Lists { get; set; } = new List<KanbanList>();
        public virtual ICollection<BoardMembership> Memberships { get; set; } = new List<BoardMembership>();
    }
}