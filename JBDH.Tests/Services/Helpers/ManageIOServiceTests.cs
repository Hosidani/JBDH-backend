using System;
using System.IO;
using System.Threading.Tasks;
using JBDH.Services.Helpers;
using Xunit;

namespace JBDH.Tests.Services.Helpers
{
    public class ManageIOServiceTests
    {
        private readonly ManageIOService _service;

        public ManageIOServiceTests()
        {
            _service = new ManageIOService();
        }

        [Fact]
        public async Task ExtractTextFromPdf_WithNonExistentFile_ShouldReturnErrorMessage()
        {
            var result = await _service.ExtractTextFromPdf("non_existent_file.pdf");
            Assert.Equal("CV file not found.", result);
        }

        [Fact]
        public async Task ExtractTextFromPdf_WithNullPath_ShouldReturnErrorMessage()
        {
            var result = await _service.ExtractTextFromPdf(null!);
            Assert.Equal("CV file not found.", result);
        }

        [Fact]
        public async Task ExtractTextFromPdf_WithEmptyPath_ShouldReturnErrorMessage()
        {
            var result = await _service.ExtractTextFromPdf("");
            Assert.Equal("CV file not found.", result);
        }

        [Fact]
        public async Task ExtractTextFromPdf_WithWhitespacePath_ShouldReturnErrorMessage()
        {
            var result = await _service.ExtractTextFromPdf("   ");
            Assert.Equal("CV file not found.", result);
        }

        [Fact]
        public void ExtractJsonFromCodeBlock_WithCodeBlock_ShouldExtractJson()
        {
            var input = "```json\n{\"key\": \"value\"}\n```";
            var result = _service.ExtractJsonFromCodeBlock(input);
            Assert.Equal("{\"key\": \"value\"}", result.Trim());
        }

        [Fact]
        public void ExtractJsonFromCodeBlock_WithoutCodeBlock_ShouldReturnInput()
        {
            var input = "{\"key\": \"value\"}";
            var result = _service.ExtractJsonFromCodeBlock(input);
            Assert.Equal(input, result);
        }

        [Fact]
        public void ExtractInnerJsonFromJobAIResponse_WithOpenAIFormat_ShouldExtractJson()
        {
            var aiResponse = "{\"choices\":[{\"message\":{\"content\":\"Here is the result: {\\\"foo\\\":\\\"bar\\\"}\"}}]}";
            var result = _service.ExtractInnerJsonFromJobAIResponse(aiResponse);
            Assert.Contains("\"foo\":\"bar\"", result);
        }

        [Fact]
        public void ExtractInnerJsonFromJobAIResponse_WithPlainJson_ShouldReturnJson()
        {
            var json = "{\"foo\":\"bar\"}";
            var result = _service.ExtractInnerJsonFromJobAIResponse(json);
            Assert.Equal(json, result);
        }

        // Playwright-based tests are best skipped or mocked in CI environments.
        // [Fact(Skip = "Playwright/browser tests are not suitable for CI environments")]
        // public async Task ExtractTextFromWebPage_WithValidUrl_ShouldExtractText()
        // {
        //     var url = "https://example.com";
        //     var result = await _service.ExtractTextFromWebPage(url);
        //     Assert.NotNull(result);
        // }
    }
}