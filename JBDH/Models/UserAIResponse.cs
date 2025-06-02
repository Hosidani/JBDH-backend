using JBDH.Models;

public class UserAIResponse : AIResponse
{
    public int UserId { get; set; }
    public User User { get; set; }
}