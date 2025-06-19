using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.Authentication;
using JBDH.Services.Authentication;
using JBDH.Services.Notifications;
using System.Security.Claims;

namespace JBDH.Tests.Services.Authentication
{
    public class PasswordResetServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpRequest> _mockHttpRequest;
        private readonly PasswordResetService _passwordResetService;

        public PasswordResetServiceTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Set up HttpContext mocking
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpRequest = new Mock<HttpRequest>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockEmailService = new Mock<IEmailService>();

            // Set up HttpContext properties
            _mockHttpRequest.Setup(x => x.Scheme).Returns("https");
            _mockHttpRequest.Setup(x => x.Host).Returns(new HostString("localhost", 5000));
            _mockHttpContext.Setup(x => x.Request).Returns(_mockHttpRequest.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

            _passwordResetService = new PasswordResetService(_context, _mockEmailService.Object, _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_WithValidEmail_ShouldCreateResetTokenAndSendEmail()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
                Location = "Test Location",
                IsBanned = false
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _passwordResetService.RequestPasswordResetAsync(email);

            // Assert
            var resetEntry = await _context.PasswordResets.FirstOrDefaultAsync(p => p.UserId == user.Id);
            Assert.NotNull(resetEntry);
            Assert.Equal(user.Id, resetEntry.UserId);
            Assert.Equal(user, resetEntry.User);
            Assert.NotNull(resetEntry.Token);
            Assert.True(resetEntry.ExpirationDate > DateTime.UtcNow);
            Assert.False(resetEntry.IsUsed);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                email,
                "Reset Your Password",
                It.Is<string>(body => body.Contains("https://localhost:5000/Login/ResetPassword?token="))), Times.Once);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_WithNonexistentEmail_ShouldNotCreateResetToken()
        {
            // Arrange
            var email = "nonexistent@example.com";

            // Act
            await _passwordResetService.RequestPasswordResetAsync(email);

            // Assert
            var resetEntries = await _context.PasswordResets.ToListAsync();
            Assert.Empty(resetEntries);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_WithBannedUser_ShouldNotCreateResetToken()
        {
            // Arrange
            var email = "banned@example.com";
            var user = new User
            {
                Id = 1,
                Username = "banneduser",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
                Location = "Banned Location",
                IsBanned = true
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _passwordResetService.RequestPasswordResetAsync(email);

            // Assert
            var resetEntries = await _context.PasswordResets.ToListAsync();
            Assert.Empty(resetEntries);

            _mockEmailService.Verify(x => x.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithValidToken_ShouldResetPasswordAndRemoveToken()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
                Location = "Test Location",
                IsBanned = false
            };
            await _context.Users.AddAsync(user);

            var token = Guid.NewGuid().ToString();
            var resetEntry = new PasswordReset
            {
                UserId = user.Id,
                User = user,
                Token = token,
                ExpirationDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };
            await _context.PasswordResets.AddAsync(resetEntry);
            await _context.SaveChangesAsync();

            var newPassword = "NewPassword123!";

            // Act
            var result = await _passwordResetService.ResetPasswordAsync(token, newPassword);

            // Assert
            Assert.True(result);

            // Verify password was changed
            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.NotNull(updatedUser);
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updatedUser.PasswordHash));

            // Verify token was removed
            var remainingTokens = await _context.PasswordResets.ToListAsync();
            Assert.Empty(remainingTokens);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnFalse()
        {
            // Arrange
            var invalidToken = "invalid-token";
            var newPassword = "NewPassword123!";

            // Act
            var result = await _passwordResetService.ResetPasswordAsync(invalidToken, newPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithExpiredToken_ShouldReturnFalse()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
                Location = "Test Location",
                IsBanned = false
            };
            await _context.Users.AddAsync(user);

            var token = Guid.NewGuid().ToString();
            var resetEntry = new PasswordReset
            {
                UserId = user.Id,
                User = user,
                Token = token,
                ExpirationDate = DateTime.UtcNow.AddHours(-1), // Expired
                IsUsed = false
            };
            await _context.PasswordResets.AddAsync(resetEntry);
            await _context.SaveChangesAsync();

            var newPassword = "NewPassword123!";

            // Act
            var result = await _passwordResetService.ResetPasswordAsync(token, newPassword);

            // Assert
            Assert.False(result);

            // Verify password was not changed
            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.NotNull(updatedUser);
            Assert.True(BCrypt.Net.BCrypt.Verify("OldPassword123!", updatedUser.PasswordHash));

            // Verify token still exists
            var remainingTokens = await _context.PasswordResets.ToListAsync();
            Assert.Single(remainingTokens);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithNullUser_ShouldReturnFalse()
        {
            // Arrange
            var token = Guid.NewGuid().ToString();
            var resetEntry = new PasswordReset
            {
                UserId = 999, // Non-existent user ID
                User = null!, // This would cause issues in real scenario
                Token = token,
                ExpirationDate = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };
            await _context.PasswordResets.AddAsync(resetEntry);
            await _context.SaveChangesAsync();

            var newPassword = "NewPassword123!";

            // Act
            var result = await _passwordResetService.ResetPasswordAsync(token, newPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_ShouldGenerateUniqueTokens()
        {
            // Arrange
            var email = "test@example.com";
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
                Location = "Test Location",
                IsBanned = false
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _passwordResetService.RequestPasswordResetAsync(email);
            await _passwordResetService.RequestPasswordResetAsync(email);

            // Assert
            var resetEntries = await _context.PasswordResets.Where(p => p.UserId == user.Id).ToListAsync();
            Assert.Equal(2, resetEntries.Count);
            Assert.NotEqual(resetEntries[0].Token, resetEntries[1].Token);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 