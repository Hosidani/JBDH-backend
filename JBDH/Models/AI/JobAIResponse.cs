using System;

namespace JBDH.Models
{
    public class JobAIResponse
    {
        public int Id { get; set; }
        public string JobId { get; set; } = string.Empty;
        public string ResponseText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}