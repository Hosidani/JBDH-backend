namespace JBDH.Services
{
    public interface IManageInputService
    {
        Task<string> ExtractTextFromPdf(string pdfFilePath);
    }
}