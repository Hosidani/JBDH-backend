using JBDH.Models;

public class SearchCandidateViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Location { get; set; }
    public UserAIResponse UserAIResponse { get; set; }
    public int Priority { get; set; }
}