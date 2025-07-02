using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JBDH.Models.AI
{
    public class JobAIResponse
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        
        public string ResponseText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}