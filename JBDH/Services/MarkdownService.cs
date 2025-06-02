using Markdig;

public static class MarkdownService
{
    public static string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown ?? "");
    }
}