using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.Dataflow;
using JBDH.Models.ViewModels;
using JBDH.Services.DataFlow;
using JBDH.Models.User;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DataFlowModel = JBDH.Models.Dataflow.DataFlow;

namespace JBDH.Tests.Services.DataFlow
{
    public class DataFlowServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly DataFlowService _dataFlowService;
        private readonly User _testUser1;
        private readonly User _testUser2;

        public DataFlowServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _dataFlowService = new DataFlowService(_context);

            // Create test users
            _testUser1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@test.com",
                PasswordHash = "hash1",
                Location = "Stockholm"
            };

            _testUser2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@test.com",
                PasswordHash = "hash2",
                Location = "Gothenburg"
            };

            _context.Users.AddRange(_testUser1, _testUser2);
            _context.SaveChanges();
        }

        [Fact]
        public void GetByUserId_WithValidUserId_ShouldReturnUserPosts()
        {
            // Arrange
            var dataFlow1 = new DataFlowModel
            {
                Id = 1,
                UserId = 1,
                ContentType = "Post",
                Content = "First post",
                CreatedAt = DateTime.Now.AddHours(-2)
            };

            var dataFlow2 = new DataFlowModel
            {
                Id = 2,
                UserId = 1,
                ContentType = "Post",
                Content = "Second post",
                CreatedAt = DateTime.Now.AddHours(-1)
            };

            var dataFlow3 = new DataFlowModel
            {
                Id = 3,
                UserId = 2,
                ContentType = "Post",
                Content = "Other user post",
                CreatedAt = DateTime.Now
            };

            _context.DataFlows.AddRange(dataFlow1, dataFlow2, dataFlow3);
            _context.SaveChanges();

            // Act
            var result = _dataFlowService.GetByUserId(1);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Second post", result[0].Content); // Newest first
            Assert.Equal("First post", result[1].Content);
            Assert.All(result, df => Assert.Equal(1, df.UserId));
        }

        [Fact]
        public void GetByUserId_WithNonExistentUserId_ShouldReturnEmptyList()
        {
            // Act
            var result = _dataFlowService.GetByUserId(999);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task CreateAsync_WithValidModel_ShouldCreateDataFlow()
        {
            // Arrange
            var model = new DataFlowViewModel
            {
                ContentType = "Post",
                Content = "Test post content"
            };

            // Act
            await _dataFlowService.CreateAsync(model, 1);

            // Assert
            var created = _context.DataFlows.FirstOrDefault(df => df.UserId == 1);
            Assert.NotNull(created);
            Assert.Equal("Post", created.ContentType);
            Assert.Equal("Test post content", created.Content);
            Assert.Equal(1, created.UserId);
        }

        [Fact]
        public async Task CreateAsync_WithNullModel_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() => 
                _dataFlowService.CreateAsync(null!, 1));
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldDeleteDataFlow()
        {
            // Arrange
            var dataFlow = new DataFlowModel
            {
                Id = 1,
                UserId = 1,
                ContentType = "Post",
                Content = "To be deleted",
                CreatedAt = DateTime.Now
            };

            _context.DataFlows.Add(dataFlow);
            _context.SaveChanges();

            // Act
            await _dataFlowService.DeleteAsync(1);

            // Assert
            var deleted = _context.DataFlows.Find(1);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldNotThrowException()
        {
            // Act & Assert
            await _dataFlowService.DeleteAsync(999);
            // Should not throw any exception
        }

        [Fact]
        public async Task AddCommentAsync_WithValidData_ShouldAddComment()
        {
            // Arrange
            var parentPost = new DataFlowModel
            {
                Id = 1,
                UserId = 1,
                ContentType = "Post",
                Content = "Parent post",
                CreatedAt = DateTime.Now
            };

            _context.DataFlows.Add(parentPost);
            _context.SaveChanges();

            // Act
            await _dataFlowService.AddCommentAsync(1, 2, "Test comment");

            // Assert
            var comment = _context.DataFlows.FirstOrDefault(df => df.ParentDataFlowId == 1);
            Assert.NotNull(comment);
            Assert.Equal("Comment", comment.ContentType);
            Assert.Equal("Test comment", comment.Content);
            Assert.Equal(2, comment.UserId);
            Assert.Equal(1, comment.ParentDataFlowId);
        }

        [Fact]
        public async Task AddCommentAsync_WithNonExistentParent_ShouldNotAddComment()
        {
            // Act
            await _dataFlowService.AddCommentAsync(999, 1, "Test comment");

            // Assert
            var comment = _context.DataFlows.FirstOrDefault(df => df.ParentDataFlowId == 999);
            Assert.Null(comment);
        }

        [Fact]
        public async Task TryAddCommentAsync_WithValidComment_ShouldReturnTrue()
        {
            // Arrange
            var parentPost = new DataFlowModel
            {
                Id = 1,
                UserId = 1,
                ContentType = "Post",
                Content = "Parent post",
                CreatedAt = DateTime.Now
            };

            _context.DataFlows.Add(parentPost);
            _context.SaveChanges();

            // Act
            var result = await _dataFlowService.TryAddCommentAsync(1, 2, "Valid comment");

            // Assert
            Assert.True(result);
            var comment = _context.DataFlows.FirstOrDefault(df => df.ParentDataFlowId == 1);
            Assert.NotNull(comment);
        }

        [Fact]
        public async Task TryAddCommentAsync_WithEmptyComment_ShouldReturnFalse()
        {
            // Act
            var result = await _dataFlowService.TryAddCommentAsync(1, 1, "");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TryAddCommentAsync_WithWhitespaceComment_ShouldReturnFalse()
        {
            // Act
            var result = await _dataFlowService.TryAddCommentAsync(1, 1, "   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetAllRecursive_WithNestedStructure_ShouldReturnHierarchicalData()
        {
            // Arrange
            var parentPost = new DataFlowModel
            {
                Id = 1,
                UserId = 1,
                ContentType = "Post",
                Content = "Parent post",
                CreatedAt = DateTime.Now.AddHours(-2)
            };

            var comment1 = new DataFlowModel
            {
                Id = 2,
                UserId = 2,
                ContentType = "Comment",
                Content = "First comment",
                ParentDataFlowId = 1,
                CreatedAt = DateTime.Now.AddHours(-1)
            };

            var comment2 = new DataFlowModel
            {
                Id = 3,
                UserId = 1,
                ContentType = "Comment",
                Content = "Second comment",
                ParentDataFlowId = 1,
                CreatedAt = DateTime.Now
            };

            var replyToComment = new DataFlowModel
            {
                Id = 4,
                UserId = 2,
                ContentType = "Comment",
                Content = "Reply to comment",
                ParentDataFlowId = 2,
                CreatedAt = DateTime.Now.AddMinutes(30)
            };

            _context.DataFlows.AddRange(parentPost, comment1, comment2, replyToComment);
            _context.SaveChanges();

            // Act
            var result = _dataFlowService.GetAllRecursive();

            // Assert
            Assert.Single(result);
            var root = result[0];
            Assert.Equal("Parent post", root.Content);
            Assert.Equal(2, root.RelatedDataFlows.Count);
            Assert.Equal("First comment", root.RelatedDataFlows.First().Content);
            Assert.Equal("Second comment", root.RelatedDataFlows.Last().Content);
            Assert.Single(root.RelatedDataFlows.First().RelatedDataFlows);
            Assert.Equal("Reply to comment", root.RelatedDataFlows.First().RelatedDataFlows.First().Content);
        }

        [Fact]
        public void GetAllRecursive_WithNoData_ShouldReturnEmptyList()
        {
            // Act
            var result = _dataFlowService.GetAllRecursive();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllViewModels_WithData_ShouldReturnViewModels()
        {
            // Arrange
            var parentPost = new DataFlowModel
            {
                Id = 1,
                UserId = 1,
                ContentType = "Post",
                Content = "Parent post",
                CreatedAt = DateTime.Now
            };

            var comment = new DataFlowModel
            {
                Id = 2,
                UserId = 2,
                ContentType = "Comment",
                Content = "Test comment",
                ParentDataFlowId = 1,
                CreatedAt = DateTime.Now.AddMinutes(5)
            };

            _context.DataFlows.AddRange(parentPost, comment);
            _context.SaveChanges();

            // Act
            var result = _dataFlowService.GetAllViewModels();

            // Assert
            Assert.Single(result);
            var viewModel = result[0];
            Assert.Equal(1, viewModel.Id);
            Assert.Equal("Parent post", viewModel.Content);
            Assert.Equal("Post", viewModel.ContentType);
            Assert.Equal("user1", viewModel.Username);
            Assert.Single(viewModel.Replies);
            Assert.Equal("Test comment", viewModel.Replies[0].Content);
        }

        [Fact]
        public void DeleteDataFlow_WithValidId_ShouldDeleteDataFlow()
        {
            // Arrange
            var dataFlow = new DataFlowModel
            {
                Id = 1,
                UserId = 1,
                ContentType = "Post",
                Content = "To be deleted",
                CreatedAt = DateTime.Now
            };

            _context.DataFlows.Add(dataFlow);
            _context.SaveChanges();

            // Act
            _dataFlowService.DeleteDataFlow(1);

            // Assert
            var deleted = _context.DataFlows.Find(1);
            Assert.Null(deleted);
        }

        [Fact]
        public void DeleteDataFlow_WithNonExistentId_ShouldNotThrowException()
        {
            // Act & Assert
            _dataFlowService.DeleteDataFlow(999);
            // Should not throw any exception
        }

        [Fact]
        public void DeleteDataFlow_WithComments_ShouldDeleteOnlyParent()
        {
            // Arrange
            var parentPost = new DataFlowModel
            {
                Id = 1,
                UserId = 1,
                ContentType = "Post",
                Content = "Parent post",
                CreatedAt = DateTime.Now
            };

            var comment = new DataFlowModel
            {
                Id = 2,
                UserId = 2,
                ContentType = "Comment",
                Content = "Test comment",
                ParentDataFlowId = 1,
                CreatedAt = DateTime.Now.AddMinutes(5)
            };

            _context.DataFlows.AddRange(parentPost, comment);
            _context.SaveChanges();

            // Act
            _dataFlowService.DeleteDataFlow(1);

            // Assert
            var deletedParent = _context.DataFlows.Find(1);
            var commentStillExists = _context.DataFlows.Find(2);
            Assert.Null(deletedParent);
            Assert.NotNull(commentStillExists);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
} 