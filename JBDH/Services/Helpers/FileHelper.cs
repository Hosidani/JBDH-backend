using Microsoft.AspNetCore.Http;
using Xceed.Words.NET; // DocX
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using System.Text;

namespace JBDH.Services.Helpers
{
    public static class FileHelper
    {
        public static async Task<string?> SaveUploadedFileAsync(IFormFile file, string webRootPath, string subFolder = "uploads")
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(webRootPath, subFolder);
            Directory.CreateDirectory(uploadsFolder);
            var savedPath = Path.Combine(uploadsFolder, Path.GetFileName(file.FileName));
            using (var stream = new FileStream(savedPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return savedPath;
        }

        public static string ExtractText(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".docx")
            {
                using (var doc = DocX.Load(filePath))
                {
                    return doc.Text;
                }
            }
            else if (ext == ".pdf")
            {
                // Enkel PDF-text-extraktion, byt till ditt nuvarande PDF-bibliotek om du har ett
                using (var pdf = PdfReader.Open(filePath, PdfDocumentOpenMode.Import))
                {
                    var text = new StringBuilder();
                    foreach (var page in pdf.Pages)
                    {
                        var content = ContentReader.ReadContent(page);
                        text.Append(ExtractTextFromContent(content));
                    }
                    return text.ToString();
                }
            }
            else
            {
                throw new NotSupportedException("Endast PDF och DOCX stöds.");
            }
        }

        // Hjälpmetod för PDFSharp
        private static string ExtractTextFromContent(CObject cObject)
        {
            var result = new StringBuilder();
            if (cObject is COperator op && op.OpCode.Name == OpCodeName.Tj.ToString())
            {
                foreach (var operand in op.Operands)
                {
                    if (operand is CString str)
                        result.Append(str.Value);
                }
            }
            else if (cObject is CSequence seq)
            {
                foreach (var element in seq)
                    result.Append(ExtractTextFromContent(element));
            }
            return result.ToString();
        }
    }
}