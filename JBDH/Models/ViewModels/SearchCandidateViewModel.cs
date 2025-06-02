public class SearchCandidateViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Location { get; set; }
    public string AIResponse { get; set; }
    public DateTime AIResponseCreatedAt { get; set; }
    public int Priority { get; set; }
}