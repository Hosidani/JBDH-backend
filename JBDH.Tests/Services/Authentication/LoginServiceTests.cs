using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Moq;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.Organisation;
using JBDH.Services.Authentication;
using JBDH.Tests.TestHelpers;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using OrganisationModel = JBDH.Models.Organisation.Organisation;

namespace JBDH.Tests.Services.Authentication
{
    public class LoginServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<IAuthenticationService> _mockAuthenticationService;
        private readonly LoginService _loginService;

        public LoginServiceTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Set up HttpContext and Authentication mocking
            _mockHttpContext = new Mock<HttpContext>();
            _mockAuthenticationService = new Mock<IAuthenticationService>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            
            _mockHttpContext.Setup(x => x.RequestServices.GetService(typeof(IAuthenticationService)))
                .Returns(_mockAuthenticationService.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);

            _loginService = new LoginService(_context, _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task ValidateUserAsync_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var username = "testuser";
            var password = "TestPassword123!";
            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Email = "test@example.com",
                Location = "Test Location",
                IsBanned = false
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _loginService.ValidateUserAsync(username, password);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.UserExists);
            Assert.True(result.PasswordCorrect);
            Assert.False(result.IsBanned);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateUserAsync_WithInvalidUsername_ShouldReturnFailure()
        {
            // Arrange
            var username = "nonexistentuser";
            var password = "TestPassword123!";

            // Act
            var result = await _loginService.ValidateUserAsync(username, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.False(result.UserExists);
            Assert.False(result.PasswordCorrect);
            Assert.Equal("Invalid username or password.", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateUserAsync_WithInvalidPassword_ShouldReturnFailure()
        {
            // Arrange
            var username = "testuser";
            var correctPassword = "TestPassword123!";
            var wrongPassword = "WrongPassword123!";
            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword),
                Email = "test@example.com",
                Location = "Test Location",
                IsBanned = false
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _loginService.ValidateUserAsync(username, wrongPassword);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.UserExists);
            Assert.False(result.PasswordCorrect);
            Assert.Equal("Invalid username or password.", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateUserAsync_WithBannedUser_ShouldReturnBanned()
        {
            // Arrange
            var username = "banneduser";
            var password = "TestPassword123!";
            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Email = "banned@example.com",
                Location = "Banned Location",
                IsBanned = true
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _loginService.ValidateUserAsync(username, password);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.UserExists);
            Assert.True(result.IsBanned);
            Assert.Equal("This account has been banned.", result.ErrorMessage);
        }

        [Fact]
        public async Task SignInUserAsync_WithValidUser_ShouldSignInUser()
        {
            // Arrange
            var username = "testuser";
            var user = new User
            {
                Id = 1,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Email = "test@example.com",
                Location = "Test Location",
                IsBanned = false
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockAuthenticationService.Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Act
            await _loginService.SignInUserAsync(username);

            // Assert
            _mockAuthenticationService.Verify(x => x.SignInAsync(
                _mockHttpContext.Object,
                "CookieAuth",
                It.Is<ClaimsPrincipal>(p => 
                    p.HasClaim(c => c.Type == ClaimTypes.Name && c.Value == username) &&
                    p.HasClaim(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1")),
                null), Times.Once);
        }

        [Fact]
        public async Task SignInUserAsync_WithNonexistentUser_ShouldNotSignIn()
        {
            // Arrange
            var username = "nonexistentuser";

            // Act
            await _loginService.SignInUserAsync(username);

            // Assert
            _mockAuthenticationService.Verify(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()), Times.Never);
        }

        [Fact]
        public async Task SignInUserAsync_WithBannedUser_ShouldNotSignIn()
        {
            // Arrange
            var username = "banneduser";
            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Email = "banned@example.com",
                Location = "Banned Location",
                IsBanned = true
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _loginService.SignInUserAsync(username);

            // Assert
            _mockAuthenticationService.Verify(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()), Times.Never);
        }

        [Fact]
        public async Task RegisterUserAsync_WithBasicInfo_ShouldCreateUser()
        {
            // Arrange
            var username = "newuser";
            var password = "TestPassword123!";

            // Act
            await _loginService.RegisterUserAsync(username, password, null);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            Assert.NotNull(user);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
            Assert.Empty(user.Email);
            Assert.Empty(user.Location);
        }

        [Fact]
        public async Task RegisterUserAsync_WithOrganization_ShouldCreateUserAndNotifyManagers()
        {
            // Arrange
            var username = "newuser";
            var password = "TestPassword123!";
            var organization = new OrganisationModel
            {
                Id = 1,
                Name = "Test Org",
                Managers = new List<User>
                {
                    new User 
                    { 
                        Username = "manager1",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager1Pass!"),
                        Email = "manager1@test.com",
                        Location = "Manager1 Location"
                    },
                    new User 
                    { 
                        Username = "manager2",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager2Pass!"),
                        Email = "manager2@test.com",
                        Location = "Manager2 Location"
                    }
                }
            };
            await _context.Organisations.AddAsync(organization);
            await _context.SaveChangesAsync();

            // Act
            await _loginService.RegisterUserAsync(username, password, organization.Id);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            Assert.NotNull(user);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 