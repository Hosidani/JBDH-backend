using System.Net;
using System.Text;
using JBDH.Models.AI;
using JBDH.Models.User;
using JBDH.Services.AICommunications;
using JBDH.Services.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;

namespace JBDH.Tests.Services.AICommunications
{
    public class AIApiClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IManageIOService> _mockManageIOService;
        private readonly Mock<IAIRequestBuilderService> _mockAIRequestBuilderService;
        private readonly AIConfig _aiConfig;
        private readonly AIApiClient _aiApiClient;
        private readonly User _testUser;
        private readonly ManageIOService _manageIOService;

        public AIApiClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockManageIOService = new Mock<IManageIOService>();
            _mockAIRequestBuilderService = new Mock<IAIRequestBuilderService>();

            _aiConfig = new AIConfig
            {
                OpenAIKey = "test-api-key",
                OpenAIUrl = "https://api.openai.com/v1/chat/completions"
            };

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _aiApiClient = new AIApiClient(httpClient, _aiConfig, _mockManageIOService.Object, _mockAIRequestBuilderService.Object);

            _testUser = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Location = "Test City"
            };
        }

        [Fact]
        public async Task GetAIResponse_WithValidRequest_ShouldReturnAIResponse()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var expectedResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = "AI response content"
                        }
                    }
                }
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockAIRequestBuilderService.Setup(x => x.AIRequestBuilder(request))
                .ReturnsAsync(new StringContent("test content"));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("AI response content", result);
            _mockAIRequestBuilderService.Verify(x => x.AIRequestBuilder(request), Times.Once);
        }

        [Fact]
        public async Task GetAIResponse_WithNullRequest_ShouldReturnError()
        {
            // Act
            var result = await _aiApiClient.GetAIResponse(null!);

            // Assert
            Assert.Equal("Request is null", result);
        }

        [Fact]
        public async Task GetAIResponse_WithNullUser_ShouldReturnError()
        {
            // Arrange
            var request = new AIRequest
            {
                User = null!,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            // Act
            var result = await _aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("User is null", result);
        }

        [Fact]
        public async Task GetAIResponse_WithInvalidCommandId_ShouldReturnError()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = -1,
                JobAdUrl = "https://example.com/job"
            };

            // Act
            var result = await _aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("CommandId is invalid", result);
        }

        [Fact]
        public async Task GetAIResponse_WithNullAIRequestBuilderService_ShouldReturnError()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            var aiApiClient = new AIApiClient(httpClient, _aiConfig, _mockManageIOService.Object, null!);

            // Act
            var result = await aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("AIHelperSwitchService is not initialized", result);
        }

        [Fact]
        public async Task GetAIResponse_WithNullContent_ShouldReturnDefault()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var expectedResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = (string?)null
                        }
                    }
                }
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockAIRequestBuilderService.Setup(x => x.AIRequestBuilder(request))
                .ReturnsAsync(new StringContent("test content"));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public async Task GetAIResponse_WithHttpError_ShouldThrowException()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

            _mockAIRequestBuilderService.Setup(x => x.AIRequestBuilder(request))
                .ReturnsAsync(new StringContent("test content"));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _aiApiClient.GetAIResponse(request));
        }

        [Fact]
        public async Task GetAIResponse_WithEmptyResponseBody_ShouldReturnError()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json")
            };

            _mockAIRequestBuilderService.Setup(x => x.AIRequestBuilder(request))
                .ReturnsAsync(new StringContent("test content"));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("No response", result);
        }

        [Fact]
        public async Task GetAIResponse_WithInvalidJsonResponse_ShouldReturnDefault()
        {
            // Arrange
            var request = new AIRequest { User = _testUser, CommandId = 0 };
            var invalidJson = "invalid json content";

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(invalidJson)
                });

            _mockAIRequestBuilderService.Setup(x => x.AIRequestBuilder(request))
                .ReturnsAsync(new StringContent("test"));

            // Act & Assert
            await Assert.ThrowsAsync<Newtonsoft.Json.JsonReaderException>(() => 
                _aiApiClient.GetAIResponse(request));
        }

        [Fact]
        public async Task GetAIResponse_WithMissingChoices_ShouldReturnDefault()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var expectedResponse = new { };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockAIRequestBuilderService.Setup(x => x.AIRequestBuilder(request))
                .ReturnsAsync(new StringContent("test content"));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("No response", result);
        }

        [Fact]
        public async Task GetAIResponse_WithMissingMessage_ShouldReturnDefault()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var expectedResponse = new
            {
                choices = new[]
                {
                    new { }
                }
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockAIRequestBuilderService.Setup(x => x.AIRequestBuilder(request))
                .ReturnsAsync(new StringContent("test content"));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("No response", result);
        }

        [Fact]
        public async Task GetAIResponse_WithMissingContent_ShouldReturnDefault()
        {
            // Arrange
            var request = new AIRequest
            {
                User = _testUser,
                CommandId = 1,
                JobAdUrl = "https://example.com/job"
            };

            var expectedResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new { }
                    }
                }
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockAIRequestBuilderService.Setup(x => x.AIRequestBuilder(request))
                .ReturnsAsync(new StringContent("test content"));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _aiApiClient.GetAIResponse(request);

            // Assert
            Assert.Equal("No response", result);
        }
    }
}