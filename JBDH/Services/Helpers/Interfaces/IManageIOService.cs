namespace JBDH.Services.Helpers
{
    public interface IManageIOService
    {
        Task<string> ExtractTextFromWebPage(string url);
        Task<string> ExtractTextFromPdf(string pdfFilePath);
        string ExtractJsonFromCodeBlock(string aiResponseText);
        string ExtractInnerJsonFromJobAIResponse(string aiResultString); 
    }
}