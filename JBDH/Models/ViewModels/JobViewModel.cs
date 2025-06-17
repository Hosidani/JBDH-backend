using System;
using System.Collections.Generic;

namespace JBDH.Models.ViewModels
{
    public class JobViewModel
    {
        public string JobId { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string WebpageUrl { get; set; }
        public DateTime? PostedDate { get; set; }
        public bool HasApplied { get; set; }

        // Optionally, include AI meta-data fields if needed
        public string LatestAIResponse { get; set; }
        // Add more properties as needed for your UI
    }
}