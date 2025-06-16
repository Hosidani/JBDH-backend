using UglyToad.PdfPig;
using System.Text;
using HtmlAgilityPack;
using Microsoft.Playwright;


namespace JBDH.Services
{
    public class ManageInputService : IManageInputService
    {
        public async Task<string> ExtractTextFromWebPage(string url)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(url);

            // Wait for the job description to appear (adjust selector if needed)
            await page.WaitForSelectorAsync("div.job-description");

            var content = await page.TextContentAsync("div.job-description");
            string text = !string.IsNullOrWhiteSpace(content) ? content : "No content found.";
            return text;
        }
        public Task<string> ExtractTextFromPdf(string pdfFilePath)
        {
            if (!File.Exists(pdfFilePath))
            {
                LoggingConfigService.ReturnError($"PDF file not found: {pdfFilePath}");
                return Task.FromResult("CV file not found.");
            }
            var sb = new StringBuilder();
            using (var document = PdfDocument.Open(pdfFilePath))
            {
                foreach (var page in document.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
            return Task.FromResult(sb.ToString());
        }
    }
}
