using UglyToad.PdfPig;
using System.Text;

namespace JBDH.Services
{
    public class ManageInputService : IManageInputService
    {
        public async Task<string> ExtractTextFromPdf(string pdfFilePath)
        {
            var sb = new StringBuilder();
            using (var document = PdfDocument.Open(pdfFilePath))
            {
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
            return sb.ToString();
        }
    }
}
