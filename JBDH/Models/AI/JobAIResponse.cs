using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.AI
{
    public class JobAIResponse
    {
        public int Id { get; set; }
        
        [ForeignKey("Job")]
        public int JobId { get; set; }
        
        public string ResponseText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Navigation property
        public virtual Job.Job? Job { get; set; }
    }
}