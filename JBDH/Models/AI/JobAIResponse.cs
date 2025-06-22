
namespace JBDH.Models.AI
{
    public class JobAIResponse
    {
        public int Id { get; set; }
        public string JobId { get; set; } = string.Empty;
        public string ResponseText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}