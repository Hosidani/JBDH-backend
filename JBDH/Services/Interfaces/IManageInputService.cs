namespace JBDH.Services
{
    public interface IManageInputService
    {
        Task<string> ExtractTextFromWebPage(string url);
        Task<string> ExtractTextFromPdf(string pdfFilePath);
    }
}