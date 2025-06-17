using JBDH.Models.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models
{
    public class Organisation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<JBDH.Models.User.User> Managers { get; set; } = new List<JBDH.Models.User.User>();

        public ICollection<int> Users { get; set; } = new List<int>();
    }
}