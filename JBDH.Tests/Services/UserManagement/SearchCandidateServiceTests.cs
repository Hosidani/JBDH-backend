using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Models.AI;
using JBDH.Models.ViewModels;
using JBDH.Services.UserManagement;
using JBDH.Services.AICommunications;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace JBDH.Tests.Services.UserManagement
{
    public class SearchCandidateServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IUserAIResponseService> _mockUserAIResponseService;
        private readonly SearchCandidateService _searchCandidateService;
        private readonly User _user1;
        private readonly User _user2;
        private readonly User _user3;
        private readonly UserAIResponse _aiResponse1;
        private readonly UserAIResponse _aiResponse2;
        private readonly UserAIResponse _aiResponse3;

        public SearchCandidateServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockUserAIResponseService = new Mock<IUserAIResponseService>();

            _searchCandidateService = new SearchCandidateService(_context, _mockUserAIResponseService.Object);

            // Create test users
            _user1 = new User
            {
                Id = 1,
                PasswordHash = "hash1",
                Email = "dev1@test.com",
                Location = "Stockholm",
                Searchable = true
            };

            _user2 = new User
            {
                Id = 2,
                PasswordHash = "hash2",
                Email = "dev2@test.com",
                Location = "Gothenburg",
                Searchable = true
            };

            _user3 = new User
            {
                Id = 3,
                PasswordHash = "hash3",
                Email = "dev3@test.com",
                Location = "Stockholm",
                Searchable = false
            };

            // Create AI responses
            _aiResponse1 = new UserAIResponse
            {
                Id = 1,
                UserId = _user1.Id,
                User = _user1,
                ResponseText = "Experienced C# developer with 5 years of experience",
                CreatedAt = DateTime.UtcNow
            };

            _aiResponse2 = new UserAIResponse
            {
                Id = 2,
                UserId = _user2.Id,
                User = _user2,
                ResponseText = "Full-stack developer with React and .NET experience",
                CreatedAt = DateTime.UtcNow
            };

            _aiResponse3 = new UserAIResponse
            {
                Id = 3,
                UserId = _user3.Id,
                User = _user3,
                ResponseText = "Python developer with machine learning experience",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.AddRange(_user1, _user2, _user3);
            _context.UserAIResponses.AddRange(_aiResponse1, _aiResponse2, _aiResponse3);
            _context.SaveChanges();

            // Setup mock AI response service
            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(_user1.Id))
                .ReturnsAsync(_aiResponse1);
            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(_user2.Id))
                .ReturnsAsync(_aiResponse2);
            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(_user3.Id))
                .ReturnsAsync(_aiResponse3);
        }

        [Fact]
        public async Task SearchCandidates_WithEmptyKeywordAndLocation_ShouldReturnAllOpenForHiringUsers()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("", "");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.UserId == _user1.Id);
            Assert.Contains(result, c => c.UserId == _user2.Id);
            Assert.DoesNotContain(result, c => c.UserId == _user3.Id); // Not open for hiring
        }

        [Fact]
        public async Task SearchCandidates_WithKeyword_ShouldFilterByKeyword()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("C#", "");

            // Assert
            Assert.Single(result);
            Assert.Equal(_user1.Id, result[0].UserId);
            Assert.Equal(_user1.Email, result[0].Email);
        }

        [Fact]
        public async Task SearchCandidates_WithLocation_ShouldFilterByLocation()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("", "Stockholm");

            // Assert
            Assert.Single(result);
            Assert.Equal(_user1.Id, result[0].UserId);
            Assert.Equal("Stockholm", result[0].Location);
            Assert.Equal(1, result[0].Priority); // Priority 1 for location match
        }

        [Fact]
        public async Task SearchCandidates_WithKeywordAndLocation_ShouldFilterByBoth()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("developer", "Stockholm");

            // Assert
            Assert.Single(result);
            Assert.Equal(_user1.Id, result[0].UserId);
            Assert.Equal(1, result[0].Priority); // Priority 1 for location match
        }

        [Fact]
        public async Task SearchCandidates_WithCaseInsensitiveKeyword_ShouldMatch()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("C#", "");

            // Assert
            Assert.Single(result);
            Assert.Equal(_user1.Id, result[0].UserId);
        }

        [Fact]
        public async Task SearchCandidates_WithCaseInsensitiveLocation_ShouldMatch()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("", "stockholm");

            // Assert
            Assert.Single(result);
            Assert.Equal(_user1.Id, result[0].UserId);
            Assert.Equal(1, result[0].Priority);
        }

        [Fact]
        public async Task SearchCandidates_WithNonMatchingKeyword_ShouldReturnEmpty()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("Java", "");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchCandidates_WithNonMatchingLocation_ShouldReturnEmpty()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("", "Malmö");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchCandidates_WithNullKeyword_ShouldHandleNull()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates(null!, "");

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SearchCandidates_WithNullLocation_ShouldHandleNull()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("", null!);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SearchCandidates_WithBothNull_ShouldReturnAllOpenForHiringUsers()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates(null!, null!);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SearchCandidates_WithWhitespaceKeyword_ShouldTreatAsEmpty()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("   ", "");

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SearchCandidates_WithWhitespaceLocation_ShouldTreatAsEmpty()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("", "   ");

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task SearchCandidates_WithMultipleLocationMatches_ShouldPrioritizeCorrectly()
        {
            // Arrange - Add another user in Stockholm
            var user4 = new User
            {
                Id = 4,
                PasswordHash = "hash4",
                Email = "dev4@test.com",
                Location = "Stockholm",
                Searchable = true
            };
            var aiResponse4 = new UserAIResponse
            {
                Id = 4,
                UserId = user4.Id,
                User = user4,
                ResponseText = "Frontend developer with Angular experience",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user4);
            _context.UserAIResponses.Add(aiResponse4);
            await _context.SaveChangesAsync();

            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(user4.Id))
                .ReturnsAsync(aiResponse4);

            // Act
            var result = await _searchCandidateService.SearchCandidates("", "Stockholm");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal(1, c.Priority)); // All should have priority 1
            Assert.Contains(result, c => c.UserId == _user1.Id);
            Assert.Contains(result, c => c.UserId == user4.Id);
        }

        [Fact]
        public async Task SearchCandidates_WithMixedLocationMatches_ShouldPrioritizeCorrectly()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("developer", "");

            // Assert
            Assert.Equal(2, result.Count);
            var stockholmUser = result.First(c => c.Location == "Stockholm");
            var gothenburgUser = result.First(c => c.Location == "Gothenburg");
            Assert.Equal(2, stockholmUser.Priority); // Priority 2 when no location specified
            Assert.Equal(2, gothenburgUser.Priority); // Priority 2 when no location specified
        }

        [Fact]
        public async Task SearchCandidates_WithUserWithoutAIResponse_ShouldNotInclude()
        {
            // Arrange - Add user without AI response
            var userWithoutAI = new User
            {
                Id = 5,
                PasswordHash = "hash5",
                Email = "dev5@test.com",
                Location = "Stockholm",
                Searchable = true
            };
            _context.Users.Add(userWithoutAI);
            await _context.SaveChangesAsync();

            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(userWithoutAI.Id))
                .ReturnsAsync((UserAIResponse?)null);

            // Act
            var result = await _searchCandidateService.SearchCandidates("", "");

            // Assert
            Assert.Equal(2, result.Count); // Should not include user without AI response
            Assert.DoesNotContain(result, c => c.UserId == userWithoutAI.Id);
        }

        [Fact]
        public async Task SearchCandidates_WithEmptyAIResponse_ShouldInclude()
        {
            // Arrange - Add user with empty AI response
            var userWithEmptyAI = new User
            {
                Id = 6,
                PasswordHash = "hash6",
                Email = "dev6@test.com",
                Location = "Stockholm",
                Searchable = true
            };
            var emptyAIResponse = new UserAIResponse
            {
                Id = 6,
                UserId = userWithEmptyAI.Id,
                User = userWithEmptyAI,
                ResponseText = "",
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(userWithEmptyAI);
            _context.UserAIResponses.Add(emptyAIResponse);
            await _context.SaveChangesAsync();

            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(userWithEmptyAI.Id))
                .ReturnsAsync(emptyAIResponse);

            // Act
            var result = await _searchCandidateService.SearchCandidates("", "");

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, c => c.UserId == userWithEmptyAI.Id);
        }

        [Fact]
        public async Task SearchCandidates_WithNullAIResponse_ShouldCreateDefaultResponse()
        {
            // Arrange - Add user with null AI response
            var userWithNullAI = new User
            {
                Id = 7,
                PasswordHash = "hash7",
                Email = "dev7@test.com",
                Location = "Stockholm",
                Searchable = true
            };
            _context.Users.Add(userWithNullAI);
            await _context.SaveChangesAsync();

            _mockUserAIResponseService.Setup(x => x.GetLatestUserAIResponseAsync(userWithNullAI.Id))
                .ReturnsAsync((UserAIResponse?)null);

            // Act
            var result = await _searchCandidateService.SearchCandidates("", "");

            // Assert
            Assert.Equal(2, result.Count); // Should not include user with null AI response
            Assert.DoesNotContain(result, c => c.UserId == userWithNullAI.Id);
        }

        [Fact]
        public async Task SearchCandidates_WithSpecialCharactersInKeyword_ShouldHandleCorrectly()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates(".NET", "");

            // Assert
            Assert.Single(result);
            Assert.Equal(_user2.Id, result[0].UserId); // user2 has ".NET" in their response
        }

        [Fact]
        public async Task SearchCandidates_WithSpecialCharactersInLocation_ShouldHandleCorrectly()
        {
            // Act
            var result = await _searchCandidateService.SearchCandidates("", "Stock-holm");

            // Assert
            Assert.Empty(result); // No exact match
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}