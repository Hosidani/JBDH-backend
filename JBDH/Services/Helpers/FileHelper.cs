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
            try
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var fileInfo = new FileInfo(filePath);
                JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"ExtractText: Path={filePath}, Exists={fileInfo.Exists}, Length={fileInfo.Length}, Extension={fileInfo.Extension}");

                if (ext == ".docx")
                {
                    try
                    {
                        using (var doc = DocX.Load(filePath))
                        {
                            var text = doc.Text;
                            JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"Extracted DOCX text length: {text.Length}");
                            return text;
                        }
                    }
                    catch (Exception ex)
                    {
                        JBDH.Services.Configuration.LoggingConfigService.ReturnError($"DOCX extraction error: {ex.Message}");
                        throw;
                    }
                }
                else if (ext == ".pdf")
                {
                    try
                    {
                        var sb = new StringBuilder();
                        using (var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath))
                        {
                            int pageNum = 1;
                            foreach (var page in pdf.GetPages())
                            {
                                var pageText = page.Text;
                                JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"PDFPig page {pageNum}: {pageText?.Substring(0, Math.Min(100, pageText.Length))}");
                                sb.AppendLine(pageText);
                                pageNum++;
                            }
                        }
                        var result = sb.ToString();
                        JBDH.Services.Configuration.LoggingConfigService.ReturnInfo($"Extracted PDFPig text length: {result.Length}");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        JBDH.Services.Configuration.LoggingConfigService.ReturnError($"PDFPig extraction error: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    JBDH.Services.Configuration.LoggingConfigService.ReturnError($"Unsupported file extension: {ext}");
                    throw new NotSupportedException("Endast PDF och DOCX stöds.");
                }
            }
            catch (Exception ex)
            {
                JBDH.Services.Configuration.LoggingConfigService.ReturnError($"ExtractText general error: {ex.Message}");
                return string.Empty;
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