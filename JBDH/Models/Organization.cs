using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public ICollection<User> Managers { get; set; } // Changed to a collection

        public ICollection<User> Users { get; set; }
    }
}