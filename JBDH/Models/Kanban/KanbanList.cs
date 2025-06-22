using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.Kanban
{
    public class KanbanList
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int BoardId { get; set; }
        [Required]
        public string Name { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BoardId")]
        public virtual Board? Board { get; set; }
        public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
    }
} 