using JBDH.Models;

public class SearchCandidateViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public UserAIResponse? UserAIResponse { get; set; }
    public int Priority { get; set; }
}