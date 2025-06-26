using System;
using System.Collections.Generic;

namespace JBDH.Models.ViewModels
{
    public class JobViewModel
    {
        public int JobId { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string WebpageUrl { get; set; } = string.Empty;
        public DateTime? PostedDate { get; set; }
        public bool HasApplied { get; set; }

        // Optionally, include AI meta-data fields if needed
        public string LatestAIResponse { get; set; } = string.Empty;
        // Add more properties as needed for your UI
    }
}