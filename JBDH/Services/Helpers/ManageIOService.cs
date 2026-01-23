using UglyToad.PdfPig;
using System.Text;
using Microsoft.Playwright;
using JBDH.Services.Configuration;
using Newtonsoft.Json;

namespace JBDH.Services.Helpers
{
    public class ManageIOService : IManageIOService
    {
        /// <summary>
        /// Converts any object to a camelCase JSON object (for frontend compatibility).
        /// </summary>
        public object? ToCamelCaseObject(object? obj)
        {
            if (obj == null) return null;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, new Newtonsoft.Json.JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.None
            });
            return Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        }
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

        public string ExtractJsonFromCodeBlock(string aiResponseText)
        {
            if (string.IsNullOrWhiteSpace(aiResponseText))
                return aiResponseText;

            var trimmed = aiResponseText.Trim();
            if (trimmed.StartsWith("```"))
            {
                var lines = trimmed.Split('\n').ToList();
                lines.RemoveAt(0);
                if (lines.Count > 0 && lines[^1].Trim().StartsWith("```"))
                    lines.RemoveAt(lines.Count - 1);
                return string.Join("\n", lines).Trim();
            }
            return aiResponseText;
        }

        public string ExtractInnerJsonFromJobAIResponse(string aiResultString)
        {
            if (string.IsNullOrWhiteSpace(aiResultString))
                return string.Empty;

            // Try to parse as an outer OpenAI response
            try
            {
                dynamic? outer = JsonConvert.DeserializeObject(aiResultString);
                if (outer == null) return string.Empty;

                var choices = outer?.choices;
                if (choices != null)
                {
                    string content = outer?.choices?[0]?.message?.content != null ? outer.choices[0].message.content.ToString(): "";
                    if (!string.IsNullOrWhiteSpace(content) && content.Contains("{"))
                    {
                        var start = content.IndexOf("{");
                        var end = content.LastIndexOf("}");
                        if (start >= 0 && end > start)
                            return content.Substring(start, end - start + 1).Trim();
                        return content.Trim();
                    }
                }
            }
            catch
            {
                // If parsing fails, fall through and treat as plain JSON
            }

            // If not an OpenAI response, assume it's already clean JSON
            return aiResultString.Trim();
        }

        public Task<string> ExtractTextFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                LoggingConfigService.ReturnError($"File not found: {filePath}");
                return Task.FromResult("File not found.");
            }
            try
            {
                var text = FileHelper.ExtractText(filePath);
                return Task.FromResult(text);
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Error extracting text: {ex.Message}");
                return Task.FromResult("");
            }
        }
    }
}
