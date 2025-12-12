namespace JBDH.Services.Helpers
{
    public interface IManageIOService
    {
    Task<string> ExtractTextFromWebPage(string url);
    Task<string> ExtractTextFromFile(string pdfFilePath);
    string ExtractJsonFromCodeBlock(string aiResponseText);
    string ExtractInnerJsonFromJobAIResponse(string aiResultString);

    /// <summary>
    /// Converts any object to a camelCase JSON object (for frontend compatibility).
    /// </summary>
    object? ToCamelCaseObject(object? obj);
    }
}