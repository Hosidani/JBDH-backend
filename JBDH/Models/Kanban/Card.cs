using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.Kanban
{
    public class Card
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ListId { get; set; }
        [Required]
        public required string Title { get; set; }
        public string? Description { get; set; }
        public int? Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public int? AssignedUserId { get; set; }

        // Not mapped: set in service/controller by looking up the parent List's BoardId
        [NotMapped]
        public int BoardId { get; set; }

        // Navigation properties
        [ForeignKey("ListId")]
        public virtual KanbanList? List { get; set; }
        // Optionally, add navigation to User if needed
    }
}