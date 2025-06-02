using System;

namespace JBDH.Models
{
    public class JobAIResponse
    {
        public int Id { get; set; }
        public string JobId { get; set; }
        public string ResponseText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}