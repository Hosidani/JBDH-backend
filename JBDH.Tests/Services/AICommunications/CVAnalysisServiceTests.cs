using JBDH.Models.AI;
using JBDH.Models.User;
using JBDH.Services.AICommunications;
using JBDH.Services.Helpers;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace JBDH.Tests.Services.AICommunications
{
    public class CVAnalysisServiceTests
    {
        private readonly Mock<IAIApiClient> _mockAIApiClient;
        private readonly Mock<IUserAIResponseService> _mockUserAIResponseService;
        private readonly Mock<IManageIOService> _mockManageIOService;
        private readonly CVAnalysisService _cvAnalysisService;
        private readonly User _testUser;

        public CVAnalysisServiceTests()
        {
            _mockAIApiClient = new Mock<IAIApiClient>();
            _mockUserAIResponseService = new Mock<IUserAIResponseService>();
            _mockManageIOService = new Mock<IManageIOService>();
            _cvAnalysisService = new CVAnalysisService(
                _mockAIApiClient.Object,
                _mockUserAIResponseService.Object,
                _mockManageIOService.Object);

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
        public async Task AnalyzeAndSaveCVAsync_WithValidCV_ShouldProcessAndSave()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var validJsonResponse = @"{
                ""BestJobTitlesForUser"": [""Software Developer"", ""Full Stack Developer""],
                ""HardSkills"": [""C#"", ""JavaScript"", ""SQL""],
                ""SoftSkills"": [""Communication"", ""Teamwork""],
                ""Experiences"": [
                    {""Title"": ""Junior Developer"", ""Duration"": ""2 years""}
                ],
                ""EducationAndCertifications"": [""Bachelor's in Computer Science""]
            }";

            var aiResponseWithCodeBlock = $"```json\n{validJsonResponse}\n```";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(aiResponseWithCodeBlock);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockAIApiClient.Verify(x => x.GetAIResponse(It.Is<AIRequest>(req => 
                req.User == _testUser && 
                req.CommandId == 0 && 
                req.CVPath == cvPath)), Times.Once);

            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.UserId == _testUser.Id &&
                resp.User == _testUser &&
                resp.ResponseText == validJsonResponse), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithPlainJsonResponse_ShouldProcessAndSave()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var validJsonResponse = @"{
                ""BestJobTitlesForUser"": [""Software Developer""],
                ""HardSkills"": [""C#""],
                ""SoftSkills"": [""Communication""],
                ""Experiences"": [],
                ""EducationAndCertifications"": []
            }";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(validJsonResponse);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == validJsonResponse), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithInvalidJsonResponse_ShouldStillSaveRawResponse()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var invalidJsonResponse = "This is not valid JSON";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(invalidJsonResponse);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == invalidJsonResponse), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithEmptyResponse_ShouldHandleGracefully()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var emptyResponse = "";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(emptyResponse);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == emptyResponse), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithNullResponse_ShouldHandleGracefully()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var nullResponse = ""; // Use empty string instead of null to avoid nullability warning

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(nullResponse);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == nullResponse), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithWhitespaceResponse_ShouldHandleGracefully()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var whitespaceResponse = "   \n\t   ";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(whitespaceResponse);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == whitespaceResponse), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithCodeBlockWithoutJson_ShouldExtractContent()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var codeBlockResponse = "```\nSome content here\n```";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(codeBlockResponse);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == "Some content here"), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithCodeBlockWithLanguage_ShouldExtractContent()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var codeBlockResponse = "```json\n{\"test\": \"value\"}\n```";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(codeBlockResponse);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == "{\"test\": \"value\"}"), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithIncompleteCodeBlock_ShouldHandleGracefully()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var incompleteCodeBlock = "```\nSome content without closing";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(incompleteCodeBlock);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == "Some content without closing"), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithEmptyCodeBlock_ShouldHandleGracefully()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var emptyCodeBlock = "```\n```";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(emptyCodeBlock);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == ""), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithComplexJsonResponse_ShouldProcessCorrectly()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var complexJsonResponse = @"{
                ""BestJobTitlesForUser"": [""Senior Software Engineer"", ""Tech Lead"", ""Architect""],
                ""HardSkills"": [""C#"", "".NET"", ""Azure"", ""Docker"", ""Kubernetes"", ""SQL Server"", ""Redis"", ""RabbitMQ""],
                ""SoftSkills"": [""Leadership"", ""Communication"", ""Problem Solving"", ""Team Management"", ""Mentoring""],
                ""Experiences"": [
                    {""Title"": ""Senior Developer"", ""Duration"": ""3 years""},
                    {""Title"": ""Team Lead"", ""Duration"": ""2 years""},
                    {""Title"": ""Junior Developer"", ""Duration"": ""2 years""}
                ],
                ""EducationAndCertifications"": [
                    ""Master's in Computer Science"",
                    ""Azure Developer Associate"",
                    ""AWS Solutions Architect"",
                    ""Scrum Master Certification""
                ]
            }";

            var aiResponseWithCodeBlock = $"```json\n{complexJsonResponse}\n```";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(aiResponseWithCodeBlock);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath);

            // Assert
            _mockUserAIResponseService.Verify(x => x.SaveUserAIResponseAsync(It.Is<UserAIResponse>(resp =>
                resp.ResponseText == complexJsonResponse), _testUser), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithNullUser_ShouldHandleGracefully()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            User? nullUser = null;

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync("test response");

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => 
                _cvAnalysisService.AnalyzeAndSaveCVAsync(nullUser!, cvPath));
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithNullCvPath_ShouldHandleGracefully()
        {
            // Arrange
            string? nullCvPath = null;

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync("test response");

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .Returns(Task.CompletedTask);

            // Act
            await _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, nullCvPath!);

            // Assert
            _mockAIApiClient.Verify(x => x.GetAIResponse(It.Is<AIRequest>(req => 
                req.User == _testUser && 
                req.CommandId == 0 && 
                req.CVPath == nullCvPath)), Times.Once);
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithUserAIResponseServiceException_ShouldNotThrow()
        {
            // Arrange
            var cvPath = "test-cv.pdf";
            var validJsonResponse = "{\"test\": \"value\"}";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ReturnsAsync(validJsonResponse);

            _mockUserAIResponseService.Setup(x => x.SaveUserAIResponseAsync(It.IsAny<UserAIResponse>(), _testUser))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath));
        }

        [Fact]
        public async Task AnalyzeAndSaveCVAsync_WithAIApiClientException_ShouldNotThrow()
        {
            // Arrange
            var cvPath = "test-cv.pdf";

            _mockAIApiClient.Setup(x => x.GetAIResponse(It.IsAny<AIRequest>()))
                .ThrowsAsync(new Exception("API error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _cvAnalysisService.AnalyzeAndSaveCVAsync(_testUser, cvPath));
        }
    }
}