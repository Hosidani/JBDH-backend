using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.Friendship;
using JBDH.Models.User;
using JBDH.Services.Friendship;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FriendshipModel = JBDH.Models.Friendship.Friendship;

namespace JBDH.Tests.Services.Friendship
{
    public class FriendServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly FriendService _friendService;
        private readonly User _user1;
        private readonly User _user2;
        private readonly User _user3;

        public FriendServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _friendService = new FriendService(_context);

            // Create test users
            _user1 = new User
            {
                Id = 1,
                Username = "user1",
                PasswordHash = "hash1",
                Email = "user1@test.com",
                Location = "Location1"
            };

            _user2 = new User
            {
                Id = 2,
                Username = "user2",
                PasswordHash = "hash2",
                Email = "user2@test.com",
                Location = "Location2"
            };

            _user3 = new User
            {
                Id = 3,
                Username = "user3",
                PasswordHash = "hash3",
                Email = "user3@test.com",
                Location = "Location3"
            };

            _context.Users.AddRange(_user1, _user2, _user3);
            _context.SaveChanges();
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithValidUsers_ShouldCreateFriendship()
        {
            // Act
            var result = await _friendService.SendFriendRequestAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.True(result.Success);
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == _user1.Id && f.AddresseeId == _user2.Id);
            Assert.NotNull(friendship);
            Assert.Equal(FriendshipStatus.Pending, friendship.Status);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithSameUser_ShouldReturnFailure()
        {
            // Act
            var result = await _friendService.SendFriendRequestAsync(_user1.Id, _user1.Id);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("cannot send a friend request to yourself", result.ErrorMessage);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithNonExistentUser_ShouldReturnFailure()
        {
            // Act
            var result = await _friendService.SendFriendRequestAsync(_user1.Id, 999);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("User not found", result.ErrorMessage);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithExistingPendingRequest_ShouldReturnFailure()
        {
            // Arrange - Create a pending request
            var existingFriendship = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            _context.Friendships.Add(existingFriendship);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.SendFriendRequestAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("pending friend request already exists", result.ErrorMessage);
        }

        [Fact]
        public async Task SendFriendRequestAsync_WithReverseExistingPendingRequest_ShouldReturnFailure()
        {
            // Arrange - Create a pending request in reverse direction
            var existingFriendship = new FriendshipModel
            {
                RequesterId = _user2.Id,
                AddresseeId = _user1.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user2,
                Addressee = _user1
            };
            _context.Friendships.Add(existingFriendship);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.SendFriendRequestAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("pending friend request already exists", result.ErrorMessage);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_WithValidRequest_ShouldAcceptFriendship()
        {
            // Arrange - Create a pending friendship
            var friendship = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.AcceptFriendRequestAsync(friendship.Id);

            // Assert
            Assert.True(result);
            var updatedFriendship = await _context.Friendships.FindAsync(friendship.Id);
            Assert.NotNull(updatedFriendship);
            Assert.Equal(FriendshipStatus.Accepted, updatedFriendship.Status);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_WithNonExistentRequest_ShouldReturnFalse()
        {
            // Act
            var result = await _friendService.AcceptFriendRequestAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_WithAlreadyAcceptedRequest_ShouldReturnFalse()
        {
            // Arrange - Create an already accepted friendship
            var friendship = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.AcceptFriendRequestAsync(friendship.Id);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AreFriendsAsync_WithFriends_ShouldReturnTrue()
        {
            // Arrange - Create an accepted friendship
            var friendship = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.AreFriendsAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task AreFriendsAsync_WithReverseFriendship_ShouldReturnTrue()
        {
            // Arrange - Create an accepted friendship in reverse direction
            var friendship = new FriendshipModel
            {
                RequesterId = _user2.Id,
                AddresseeId = _user1.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user2,
                Addressee = _user1
            };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.AreFriendsAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task AreFriendsAsync_WithPendingRequest_ShouldReturnFalse()
        {
            // Arrange - Create a pending friendship
            var friendship = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.AreFriendsAsync(_user1.Id, _user2.Id);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AreFriendsAsync_WithSameUser_ShouldReturnFalse()
        {
            // Act
            var result = await _friendService.AreFriendsAsync(_user1.Id, _user1.Id);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AreFriendsAsync_WithNoFriendship_ShouldReturnFalse()
        {
            // Act
            var result = await _friendService.AreFriendsAsync(_user1.Id, _user3.Id);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetFriendsAsync_WithFriends_ShouldReturnFriendsList()
        {
            // Arrange - Create accepted friendships
            var friendship1 = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            var friendship2 = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user3.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user3
            };
            _context.Friendships.AddRange(friendship1, friendship2);
            await _context.SaveChangesAsync();

            // Act
            var friends = await _friendService.GetFriendsAsync(_user1.Id);

            // Assert
            Assert.Equal(2, friends.Count);
            Assert.Contains(friends, f => f.Id == _user2.Id);
            Assert.Contains(friends, f => f.Id == _user3.Id);
        }

        [Fact]
        public async Task GetFriendsAsync_WithReverseFriendships_ShouldReturnFriendsList()
        {
            // Arrange - Create accepted friendships in reverse direction
            var friendship1 = new FriendshipModel
            {
                RequesterId = _user2.Id,
                AddresseeId = _user1.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user2,
                Addressee = _user1
            };
            var friendship2 = new FriendshipModel
            {
                RequesterId = _user3.Id,
                AddresseeId = _user1.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user3,
                Addressee = _user1
            };
            _context.Friendships.AddRange(friendship1, friendship2);
            await _context.SaveChangesAsync();

            // Act
            var friends = await _friendService.GetFriendsAsync(_user1.Id);

            // Assert
            Assert.Equal(2, friends.Count);
            Assert.Contains(friends, f => f.Id == _user2.Id);
            Assert.Contains(friends, f => f.Id == _user3.Id);
        }

        [Fact]
        public async Task GetFriendsAsync_WithNoFriends_ShouldReturnEmptyList()
        {
            // Act
            var friends = await _friendService.GetFriendsAsync(_user1.Id);

            // Assert
            Assert.Empty(friends);
        }

        [Fact]
        public async Task GetFriendsAsync_WithPendingRequests_ShouldNotIncludeThem()
        {
            // Arrange - Create pending friendships
            var friendship1 = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            var friendship2 = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user3.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user3
            };
            _context.Friendships.AddRange(friendship1, friendship2);
            await _context.SaveChangesAsync();

            // Act
            var friends = await _friendService.GetFriendsAsync(_user1.Id);

            // Assert
            Assert.Single(friends);
            Assert.Contains(friends, f => f.Id == _user3.Id);
            Assert.DoesNotContain(friends, f => f.Id == _user2.Id);
        }

        [Fact]
        public async Task GetPendingRequestsAsync_WithPendingRequests_ShouldReturnRequestsList()
        {
            // Arrange - Create pending requests for user2
            var friendship1 = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            var friendship2 = new FriendshipModel
            {
                RequesterId = _user3.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user3,
                Addressee = _user2
            };
            var friendship3 = new FriendshipModel
            {
                RequesterId = _user2.Id,
                AddresseeId = _user1.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user2,
                Addressee = _user1
            };
            _context.Friendships.AddRange(friendship1, friendship2, friendship3);
            await _context.SaveChangesAsync();

            // Act
            var pendingRequests = await _friendService.GetPendingRequestsAsync(_user2.Id);

            // Assert
            Assert.Equal(2, pendingRequests.Count);
            Assert.Contains(pendingRequests, f => f.RequesterId == _user1.Id);
            Assert.Contains(pendingRequests, f => f.RequesterId == _user3.Id);
            Assert.DoesNotContain(pendingRequests, f => f.RequesterId == _user2.Id);
        }

        [Fact]
        public async Task GetPendingRequestsAsync_WithNoPendingRequests_ShouldReturnEmptyList()
        {
            // Act
            var pendingRequests = await _friendService.GetPendingRequestsAsync(_user1.Id);

            // Assert
            Assert.Empty(pendingRequests);
        }

        [Fact]
        public async Task GetPendingRequestsAsync_WithAcceptedRequests_ShouldNotIncludeThem()
        {
            // Arrange - Create accepted and pending requests
            var friendship1 = new FriendshipModel
            {
                RequesterId = _user1.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Friends,
                RequestedAt = DateTime.UtcNow,
                Requester = _user1,
                Addressee = _user2
            };
            var friendship2 = new FriendshipModel
            {
                RequesterId = _user3.Id,
                AddresseeId = _user2.Id,
                Status = FriendshipStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                Requester = _user3,
                Addressee = _user2
            };
            _context.Friendships.AddRange(friendship1, friendship2);
            await _context.SaveChangesAsync();

            // Act
            var pendingRequests = await _friendService.GetPendingRequestsAsync(_user2.Id);

            // Assert
            Assert.Single(pendingRequests);
            Assert.Contains(pendingRequests, f => f.RequesterId == _user3.Id);
            Assert.DoesNotContain(pendingRequests, f => f.RequesterId == _user1.Id);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}