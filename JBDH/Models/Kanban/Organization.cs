using System;
using System.ComponentModel.DataAnnotations;

namespace JBDH.Models.Kanban
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 