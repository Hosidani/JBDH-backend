using System;
using System.ComponentModel.DataAnnotations;

namespace JBDH.Models.Job
{
    public class RawJob
    {
        [Key]
           public int JobId { get; set; } // Primary key, matches JobId INT PRIMARY KEY
           public string? Title { get; set; } // title VARCHAR(255)
           public string? Description { get; set; } // description TEXT
           public string? Company { get; set; } // company VARCHAR(255)
           public string? Location { get; set; } // location VARCHAR(255)
           public DateTime? PostedDate { get; set; } // PostedDate DATE
           public string? WebpageUrl { get; set; } // WebpageUrl VARCHAR(2083)
    }
}