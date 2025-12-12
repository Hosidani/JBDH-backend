/*
using System.Text;
using JBDH.Models.AI;
using JBDH.Models.User;
using JBDH.Services.AICommunications;
using JBDH.Services.Helpers;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace JBDH.Tests.Services.AICommunications
{
    public class AIRequestBuilderServiceTests
    {
        private readonly Mock<IManageIOService> _mockManageIOService;
        private readonly AIConfig _aiConfig;
        private readonly AIRequestBuilderService _aiRequestBuilderService;
        private readonly User _testUser;

        public AIRequestBuilderServiceTests()
        {
            _mockManageIOService = new Mock<IManageIOService>();
            _aiConfig = new AIConfig
            {
                OpenAIKey = "test-api-key",
                OpenAIUrl = "https://api.openai.com/v1/chat/completions",
                OpenAIModel = "gpt-4"
            };

            _aiRequestBuilderService = new AIRequestBuilderService(_mockManageIOService.Object, _aiConfig);

            _testUser = new User
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = "hash",
                Location = "Test City"
            };
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId0_ShouldBuildCVAnalysisRequest()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 0,
                CVPath = "test-cv.pdf"
            };

            var expectedCvText = "Test CV content";
            _mockManageIOService.Setup(x => x.ExtractTextFromPdf("test-cv.pdf"))
                .ReturnsAsync(expectedCvText);

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Equal("gpt-4", requestBody?.model?.ToString());
            Assert.Equal(2, requestBody?.messages?.Count);
            Assert.Equal("system", requestBody?.messages?[0]?.role?.ToString());
            Assert.Equal("user", requestBody?.messages?[1]?.role?.ToString());
            Assert.Contains("Analyse this CV", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains(expectedCvText, requestBody?.messages?[1]?.content?.ToString());
            Assert.False((bool)requestBody?.stream);

            _mockManageIOService.Verify(x => x.ExtractTextFromPdf("test-cv.pdf"), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId1_ShouldBuildJobAdAnalysisRequest()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var expectedJobAdText = "Test job ad content";
            _mockManageIOService.Setup(x => x.ExtractTextFromWebPage("https://example.com/job"))
                .ReturnsAsync(expectedJobAdText);

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Equal("gpt-4", requestBody?.model?.ToString());
            Assert.Equal(2, requestBody?.messages?.Count);
            Assert.Equal("system", requestBody?.messages?[0]?.role?.ToString());
            Assert.Equal("user", requestBody?.messages?[1]?.role?.ToString());
            Assert.Contains("Analyze the following job advertisement", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains(expectedJobAdText, requestBody?.messages?[1]?.content?.ToString());
            Assert.False((bool)requestBody?.stream);

            _mockManageIOService.Verify(x => x.ExtractTextFromWebPage("https://example.com/job"), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId2_ShouldBuildComparisonRequest()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 2,
                CVPath = "test-cv.pdf",
                JobAdUrl = "https://example.com/job"
            };

            var expectedCvText = "Test CV content";
            var expectedJobAdText = "Test job ad content";
            
            _mockManageIOService.Setup(x => x.ExtractTextFromPdf("test-cv.pdf"))
                .ReturnsAsync(expectedCvText);
            _mockManageIOService.Setup(x => x.ExtractTextFromWebPage("https://example.com/job"))
                .ReturnsAsync(expectedJobAdText);

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Equal("gpt-4", requestBody?.model?.ToString());
            Assert.Equal(2, requestBody?.messages?.Count);
            Assert.Equal("system", requestBody?.messages?[0]?.role?.ToString());
            Assert.Equal("user", requestBody?.messages?[1]?.role?.ToString());
            Assert.Contains("You are given a job advertisement and a candidate's CV", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains(expectedCvText, requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains(expectedJobAdText, requestBody?.messages?[1]?.content?.ToString());
            Assert.False((bool)requestBody?.stream);

            _mockManageIOService.Verify(x => x.ExtractTextFromPdf("test-cv.pdf"), Times.Once);
            _mockManageIOService.Verify(x => x.ExtractTextFromWebPage("https://example.com/job"), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithInvalidCommandId_ShouldReturnNull()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 999,
                CVPath = "test-cv.pdf"
            };

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId0AndNullCVPath_ShouldHandleNullPath()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 0,
                CVPath = null
            };

            _mockManageIOService.Setup(x => x.ExtractTextFromPdf(""))
                .ReturnsAsync("");

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("Analyse this CV", requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromPdf(""), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId1AndNullJobAdUrl_ShouldHandleNullUrl()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = null
            };

            _mockManageIOService.Setup(x => x.ExtractTextFromWebPage(""))
                .ReturnsAsync("");

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("Analyze the following job advertisement", requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromWebPage(""), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId2AndNullPaths_ShouldHandleNullPaths()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 2,
                CVPath = null,
                JobAdUrl = null
            };

            _mockManageIOService.Setup(x => x.ExtractTextFromPdf(""))
                .ReturnsAsync("");
            _mockManageIOService.Setup(x => x.ExtractTextFromWebPage(""))
                .ReturnsAsync("");

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("You are given a job advertisement and a candidate's CV", requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromPdf(""), Times.Once);
            _mockManageIOService.Verify(x => x.ExtractTextFromWebPage(""), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId0AndEmptyCVText_ShouldBuildRequestWithEmptyText()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 0,
                CVPath = "test-cv.pdf"
            };

            _mockManageIOService.Setup(x => x.ExtractTextFromPdf("test-cv.pdf"))
                .ReturnsAsync("");

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("Analyse this CV", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains("CV:\n", requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromPdf("test-cv.pdf"), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId1AndEmptyJobAdText_ShouldBuildRequestWithEmptyText()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            _mockManageIOService.Setup(x => x.ExtractTextFromWebPage("https://example.com/job"))
                .ReturnsAsync("");

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("Analyze the following job advertisement", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains("Job Ad:\n", requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromWebPage("https://example.com/job"), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId2AndEmptyTexts_ShouldBuildRequestWithEmptyTexts()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 2,
                CVPath = "test-cv.pdf",
                JobAdUrl = "https://example.com/job"
            };

            _mockManageIOService.Setup(x => x.ExtractTextFromPdf("test-cv.pdf"))
                .ReturnsAsync("");
            _mockManageIOService.Setup(x => x.ExtractTextFromWebPage("https://example.com/job"))
                .ReturnsAsync("");

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("You are given a job advertisement and a candidate's CV", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains("Job Ad:\n", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains("CV:\n", requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromPdf("test-cv.pdf"), Times.Once);
            _mockManageIOService.Verify(x => x.ExtractTextFromWebPage("https://example.com/job"), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId0AndLongCVText_ShouldBuildRequestWithLongText()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 0,
                CVPath = "test-cv.pdf"
            };

            var longCvText = new string('A', 10000);
            _mockManageIOService.Setup(x => x.ExtractTextFromPdf("test-cv.pdf"))
                .ReturnsAsync(longCvText);

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("Analyse this CV", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains(longCvText, requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromPdf("test-cv.pdf"), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId1AndLongJobAdText_ShouldBuildRequestWithLongText()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var longJobAdText = new string('B', 10000);
            _mockManageIOService.Setup(x => x.ExtractTextFromWebPage("https://example.com/job"))
                .ReturnsAsync(longJobAdText);

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("Analyze the following job advertisement", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains(longJobAdText, requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromWebPage("https://example.com/job"), Times.Once);
        }

        [Fact]
        public async Task AIRequestBuilder_WithCommandId2AndLongTexts_ShouldBuildRequestWithLongTexts()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 2,
                CVPath = "test-cv.pdf",
                JobAdUrl = "https://example.com/job"
            };

            var longCvText = new string('A', 5000);
            var longJobAdText = new string('B', 5000);
            
            _mockManageIOService.Setup(x => x.ExtractTextFromPdf("test-cv.pdf"))
                .ReturnsAsync(longCvText);
            _mockManageIOService.Setup(x => x.ExtractTextFromWebPage("https://example.com/job"))
                .ReturnsAsync(longJobAdText);

            // Act
            var result = await _aiRequestBuilderService.AIRequestBuilder(request);

            // Assert
            Assert.NotNull(result);
            var content = await result.ReadAsStringAsync();
            var requestBody = JsonConvert.DeserializeObject<dynamic>(content);
            
            Assert.Contains("You are given a job advertisement and a candidate's CV", requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains(longCvText, requestBody?.messages?[1]?.content?.ToString());
            Assert.Contains(longJobAdText, requestBody?.messages?[1]?.content?.ToString());

            _mockManageIOService.Verify(x => x.ExtractTextFromPdf("test-cv.pdf"), Times.Once);
            _mockManageIOService.Verify(x => x.ExtractTextFromWebPage("https://example.com/job"), Times.Once);
        }
    }
}

*/