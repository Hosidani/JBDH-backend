using JBDH.Models;

public class JobSearchResultViewModel
{
    public Job Job { get; set; }
    public JobAIResponse LatestAIResponse { get; set; }
    public int Priority { get; set; }
    public string Description { get; set; }
    public string DescriptionHtml => MarkdownService.ToHtml(Description);
}