using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.ViewModels;
using JBDH.Models.Services;
using JBDH.Models.AI;
using JBDH.Services.UserManagement;
using JBDH.Services.Friendship;
using JBDH.Services.AICommunications;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace JBDH.Tests.Services.UserManagement
{
    public class ProfileServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IFriendService> _mockFriendService;
        private readonly Mock<IUserAIResponseService> _mockUserAIResponseService;
        private readonly ProfileService _profileService;
        private readonly User _user1;
        private readonly User _user2;

        public ProfileServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockFriendService = new Mock<IFriendService>();
            _mockUserAIResponseService = new Mock<IUserAIResponseService>();

            _profileService = new ProfileService(_context, _mockFriendService.Object, _mockUserAIResponseService.Object);

            // Create test users
            _user1 = new User
            {
                Id = 1,
                PasswordHash = "hash1",
                Email = "user1@test.com",
                Location = "Location1"
            };

            _user2 = new User
            {
                Id = 2,
                PasswordHash = "hash2",
                Email = "user2@test.com",
                Location = "Location2"
            };

            _context.Users.AddRange(_user1, _user2);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Act
            var result = await _profileService.GetUserByIdAsync(_user1.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_user1.Id, result.Id);
            Assert.Equal(_user1.Email, result.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Act
            var result = await _profileService.GetUserByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithZeroId_ShouldReturnNull()
        {
            // Act
            var result = await _profileService.GetUserByIdAsync(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithNegativeId_ShouldReturnNull()
        {
            // Act
            var result = await _profileService.GetUserByIdAsync(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProfileViewModelAsync_WithValidUsers_ShouldReturnProfileViewModel()
        {
            // Arrange
            _mockFriendService.Setup(x => x.AreFriendsAsync(_user1.Id, _user2.Id))
                .ReturnsAsync(false);

            // Act
            var result = await _profileService.GetProfileViewModelAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_user2, result.User);
            Assert.False(result.IsFriend);
            Assert.Null(result.Organisation);
        }

        [Fact]
        public async Task GetProfileViewModelAsync_WithFriends_ShouldReturnProfileViewModelWithIsFriendTrue()
        {
            // Arrange
            _mockFriendService.Setup(x => x.AreFriendsAsync(_user1.Id, _user2.Id))
                .ReturnsAsync(true);

            // Act
            var result = await _profileService.GetProfileViewModelAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_user2, result.User);
            Assert.True(result.IsFriend);
            Assert.Null(result.Organisation);
        }

        [Fact]
        public async Task GetProfileViewModelAsync_WithSameUser_ShouldReturnNull()
        {
            // Act
            var result = await _profileService.GetProfileViewModelAsync(_user1.Id, _user1.Id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProfileViewModelAsync_WithNonExistentUser_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => 
                _profileService.GetProfileViewModelAsync(_user1.Id, 999));
        }

        [Fact]
        public async Task GetMyProfileViewModelAsync_WithValidUser_ShouldReturnProfileViewModel()
        {
            // Arrange
            var userAIResponse = new UserAIResponse { Id = 1, UserId = _user1.Id, User = _user1 };
            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(_user1.Id))
                .ReturnsAsync(userAIResponse);

            // Act
            var result = await _profileService.GetMyProfileViewModelAsync(_user1.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_user1, result.User);
            Assert.False(result.IsFriend);
            Assert.Null(result.Organisation);
            Assert.Equal(userAIResponse, result.UserAIResponse);
        }

        [Fact]
        public async Task GetMyProfileViewModelAsync_WithNonExistentUser_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<System.Exception>(() => 
                _profileService.GetMyProfileViewModelAsync(999));
        }

        [Fact]
        public async Task GetMyProfileViewModelAsync_WithNullAIResponse_ShouldReturnProfileViewModel()
        {
            // Arrange
            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(_user1.Id))
                .ReturnsAsync((UserAIResponse?)null);

            // Act
            var result = await _profileService.GetMyProfileViewModelAsync(_user1.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_user1, result.User);
            Assert.False(result.IsFriend);
            Assert.Null(result.Organisation);
            Assert.Null(result.UserAIResponse);
        }

        [Fact]
        public void GetCurrentUserId_WithValidClaim_ShouldReturnUserId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _profileService.GetCurrentUserId(principal);

            // Assert
            Assert.Equal(123, result);
        }

        [Fact]
        public void GetCurrentUserId_WithInvalidClaim_ShouldReturnNull()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _profileService.GetCurrentUserId(principal);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUserId_WithMissingClaim_ShouldReturnNull()
        {
            // Arrange
            var claims = new List<Claim>();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _profileService.GetCurrentUserId(principal);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUserId_WithNullPrincipal_ShouldReturnNull()
        {
            // Act
            var result = _profileService.GetCurrentUserId(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentUserId_WithZeroId_ShouldReturnZero()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "0")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _profileService.GetCurrentUserId(principal);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetCurrentUserId_WithNegativeId_ShouldReturnNegativeId()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "-1")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var result = _profileService.GetCurrentUserId(principal);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithValidUsers_ShouldDelegateToFriendService()
        {
            // Arrange
            var expectedResult = new ServiceResult { Success = true };
            _mockFriendService.Setup(x => x.SendFriendRequestAsync(_user1.Id, _user2.Id))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _profileService.SendFriendRequestAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockFriendService.Verify(x => x.SendFriendRequestAsync(_user1.Id, _user2.Id), Times.Once);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithFailureResult_ShouldReturnFailure()
        {
            // Arrange
            var expectedResult = new ServiceResult { Success = false, ErrorMessage = "Already friends" };
            _mockFriendService.Setup(x => x.SendFriendRequestAsync(_user1.Id, _user2.Id))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _profileService.SendFriendRequestAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.False(result.Success);
            Assert.Equal("Already friends", result.ErrorMessage);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithSameUser_ShouldDelegateToFriendService()
        {
            // Arrange
            var expectedResult = new ServiceResult { Success = false, ErrorMessage = "Cannot send request to self" };
            _mockFriendService.Setup(x => x.SendFriendRequestAsync(_user1.Id, _user1.Id))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _profileService.SendFriendRequestAsync(_user1.Id, _user1.Id);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockFriendService.Verify(x => x.SendFriendRequestAsync(_user1.Id, _user1.Id), Times.Once);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}