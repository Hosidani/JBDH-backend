using JBDH.Models;

public class UserAIResponse : AIResponse
{
    public int UserId { get; set; }
    public required User User { get; set; }
    // ...eventuella andra fält...
}