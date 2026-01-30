using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.AI
{
    public class JobAIResponse
    {
        public int Id { get; set; }
        public int JobId { get; set; } 
        public string ResponseText { get; set; } = string.Empty;
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum JobStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}