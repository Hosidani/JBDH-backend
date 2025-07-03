namespace JBDH.Models.AI
{
    public class AIResponse
    {
        public required string Id { get; set; }
        public string? ResponseText { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
