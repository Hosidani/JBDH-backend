using System;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.Notifications;
using JBDH.Services.UserManagement;
using JBDH.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using JBDH.Tests.TestHelpers;

namespace JBDH.Tests.Services.UserManagement
{
    public class UserVerificationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly UserVerificationService _userVerificationService;
        private readonly TestUrlHelper _urlHelper;
        private readonly Mock<HttpRequest> _mockHttpRequest;
        private readonly User _user;

        public UserVerificationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockEmailService = new Mock<IEmailService>();
            _urlHelper = new TestUrlHelper();
            _mockHttpRequest = new Mock<HttpRequest>();

            _userVerificationService = new UserVerificationService(_context, _mockEmailService.Object);

            // Create test user
            _user = new User
            {
                Id = 1,
                PasswordHash = "hash",
                Email = "test@example.com",
                Location = "Test Location"
            };

            _context.Users.Add(_user);
            _context.SaveChanges();

            _mockHttpRequest.Setup(x => x.Scheme).Returns("https");
        }

        [Fact]
        public void VerifyEmail_WithValidToken_ShouldReturnSuccess()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var emailVerificationToken = new EmailVerificationToken
            {
                Token = token,
                UserId = _user.Id,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                IsUsed = false
            };
            _context.EmailVerificationTokens.Add(emailVerificationToken);
            _context.SaveChanges();

            // Act
            var result = _userVerificationService.VerifyEmail(token);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            var updatedToken = _context.EmailVerificationTokens.FirstOrDefault(t => t.Token == token);
            Assert.NotNull(updatedToken);
            Assert.True(updatedToken.IsUsed);
        }

        [Fact]
        public void VerifyEmail_WithInvalidToken_ShouldReturnFailure()
        {
            // Arrange
            var invalidToken = "invalid-token";

            // Act
            var result = _userVerificationService.VerifyEmail(invalidToken);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("invalid or has expired", result.ErrorMessage);
        }

        [Fact]
        public void VerifyEmail_WithExpiredToken_ShouldReturnFailure()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var emailVerificationToken = new EmailVerificationToken
            {
                Token = token,
                UserId = _user.Id,
                ExpirationDate = DateTime.UtcNow.AddDays(-1), // Expired
                IsUsed = false
            };
            _context.EmailVerificationTokens.Add(emailVerificationToken);
            _context.SaveChanges();

            // Act
            var result = _userVerificationService.VerifyEmail(token);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("invalid or has expired", result.ErrorMessage);
        }

        [Fact]
        public void VerifyEmail_WithUsedToken_ShouldReturnFailure()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var emailVerificationToken = new EmailVerificationToken
            {
                Token = token,
                UserId = _user.Id,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                IsUsed = true // Already used
            };
            _context.EmailVerificationTokens.Add(emailVerificationToken);
            _context.SaveChanges();

            // Act
            var result = _userVerificationService.VerifyEmail(token);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("invalid or has expired", result.ErrorMessage);
        }

        [Fact]
        public void VerifyEmail_WithNullToken_ShouldReturnFailure()
        {
            // Act
            var result = _userVerificationService.VerifyEmail(null!);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("invalid or has expired", result.ErrorMessage);
        }

        [Fact]
        public void VerifyEmail_WithEmptyToken_ShouldReturnFailure()
        {
            // Act
            var result = _userVerificationService.VerifyEmail("");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("invalid or has expired", result.ErrorMessage);
        }

        [Fact]
        public void VerifyEmail_WithWhitespaceToken_ShouldReturnFailure()
        {
            // Act
            var result = _userVerificationService.VerifyEmail("   ");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("invalid or has expired", result.ErrorMessage);
        }

        [Fact]
        public void VerifyEmail_WithTokenForNonExistentUser_ShouldReturnSuccess()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var emailVerificationToken = new EmailVerificationToken
            {
                Token = token,
                UserId = 999, // Non-existent user
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                IsUsed = false
            };
            _context.EmailVerificationTokens.Add(emailVerificationToken);
            _context.SaveChanges();

            // Act
            var result = _userVerificationService.VerifyEmail(token);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            var updatedToken = _context.EmailVerificationTokens.FirstOrDefault(t => t.Token == token);
            Assert.NotNull(updatedToken);
            Assert.True(updatedToken.IsUsed);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithValidEmail_ShouldReturnSuccess()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            var newToken = await _context.EmailVerificationTokens
                .FirstOrDefaultAsync(t => t.UserId == _user.Id && !t.IsUsed);
            Assert.NotNull(newToken);
            Assert.True(newToken.ExpirationDate > DateTime.UtcNow);
            Assert.True(newToken.ExpirationDate <= DateTime.UtcNow.AddDays(1));

            _mockEmailService.Verify(x => x.SendEmailAsync(email, "Verify your email", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithNonExistentEmail_ShouldReturnFailure()
        {
            // Arrange
            var email = "nonexistent@example.com";

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No user found with that email address", result.ErrorMessage);

            _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithEmptyEmail_ShouldReturnFailure()
        {
            // Arrange
            var email = "";

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No user found with that email address", result.ErrorMessage);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithNullEmail_ShouldReturnFailure()
        {
            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(null!, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No user found with that email address", result.ErrorMessage);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithWhitespaceEmail_ShouldReturnFailure()
        {
            // Arrange
            var email = "   ";

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No user found with that email address", result.ErrorMessage);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithCaseInsensitiveEmail_ShouldFindUser()
        {
            // Arrange
            var email = "TEST@EXAMPLE.COM"; // Different case

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldCreateNewToken()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.True(result.Success);

            var tokens = await _context.EmailVerificationTokens
                .Where(t => t.UserId == _user.Id)
                .ToListAsync();
            Assert.Single(tokens);
            Assert.False(tokens[0].IsUsed);
            Assert.True(tokens[0].ExpirationDate > DateTime.UtcNow);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithExistingToken_ShouldCreateNewToken()
        {
            // Arrange
            var email = "test@example.com";
            var existingToken = new EmailVerificationToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = _user.Id,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                IsUsed = false
            };
            _context.EmailVerificationTokens.Add(existingToken);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.True(result.Success);

            var tokens = await _context.EmailVerificationTokens
                .Where(t => t.UserId == _user.Id)
                .ToListAsync();
            Assert.Equal(2, tokens.Count); // Original + new token
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_ShouldSendEmailWithCorrectLink()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.True(result.Success);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                email,
                "Verify your email",
                It.Is<string>(body => body.Contains("verify"))), // Check for any verification link
                Times.Once);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithEmailServiceFailure_ShouldStillReturnSuccess()
        {
            // Arrange
            var email = "test@example.com";
            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service error"));

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public async Task ResendVerificationEmailAsync_WithSpecialCharactersInEmail_ShouldHandleCorrectly()
        {
            // Arrange
            var email = "test+tag@example.com";

            // Act
            var result = await _userVerificationService.ResendVerificationEmailAsync(email, _urlHelper, _mockHttpRequest.Object);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No user found with that email address", result.ErrorMessage);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}