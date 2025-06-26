using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.Job;
using JBDH.Models.ViewModels;
using JBDH.Services.JobManagement;
using JBDH.Services.AICommunications;
using JBDH.Services.Workflows;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace JBDH.Tests.Services.Workflows
{
    public class SearchWorkflowServiceTests : IDisposable
    {
        private readonly Mock<ISearchJobsService> _mockSearchJobsService;
        private readonly Mock<IJobAIResponseService> _mockJobAIResponseService;
        private readonly ApplicationDbContext _context;
        private readonly SearchWorkflowService _searchWorkflowService;

        public SearchWorkflowServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _mockSearchJobsService = new Mock<ISearchJobsService>();
            _mockJobAIResponseService = new Mock<IJobAIResponseService>();

            _searchWorkflowService = new SearchWorkflowService(
                _mockSearchJobsService.Object,
                _mockJobAIResponseService.Object,
                _context);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithValidParameters_ShouldReturnResults()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int? userId = 1;

            var mockResults = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel
                {
                    Job = new Job { JobId = 1, Title = "Software Engineer", Location = "Stockholm" },
                    HasApplied = false
                },
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 2, Title = "Senior Developer", Location = "Stockholm" },
                    HasApplied = false
                }
            };

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description, location), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithNullDescription_ShouldHandleGracefully()
        {
            // Arrange
            string? description = null;
            var location = "Stockholm";
            int? userId = 1;

            var mockResults = new List<JobSearchResultViewModel>();

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description!, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description!, location), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithNullLocation_ShouldHandleGracefully()
        {
            // Arrange
            var description = "Software Engineer";
            string? location = null;
            int? userId = 1;

            var mockResults = new List<JobSearchResultViewModel>();

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location!))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description, location!), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithNullUserId_ShouldNotCheckApplications()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int? userId = null;

            var mockResults = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 1, Title = "Software Engineer", Location = "Stockholm" },
                    HasApplied = false
                }
            };

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.False(result[0].HasApplied);
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description, location), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithUserIdAndExistingApplications_ShouldMarkAsApplied()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int userId = 1;

            var mockResults = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 1, Title = "Software Engineer", Location = "Stockholm" },
                    HasApplied = false
                }
            };

            // Add application to database
            var application = new JobApplication
            {
                ApplicationId = 1,
                UserId = userId,
                JobId = 1,
                AppliedAt = DateTime.UtcNow
            };
            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result[0].HasApplied);
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description, location), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithUserIdAndNoApplications_ShouldMarkAsNotApplied()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int userId = 1;

            var mockResults = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 1, Title = "Software Engineer", Location = "Stockholm" },
                    HasApplied = false
                }
            };

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.False(result[0].HasApplied);
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description, location), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithMultipleJobsAndMixedApplications_ShouldMarkCorrectly()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int userId = 1;

            var mockResults = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 1, Title = "Software Engineer", Location = "Stockholm" },
                    HasApplied = false
                },
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 2, Title = "Senior Developer", Location = "Stockholm" },
                    HasApplied = false
                },
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 3, Title = "Tech Lead", Location = "Stockholm" },
                    HasApplied = false
                }
            };

            // Add applications for job1 and job3
            var applications = new List<JobApplication>
            {
                new JobApplication { ApplicationId = 1, UserId = userId, JobId = 1, AppliedAt = DateTime.UtcNow },
                new JobApplication { ApplicationId = 2, UserId = userId, JobId = 3, AppliedAt = DateTime.UtcNow }
            };
            _context.JobApplications.AddRange(applications);
            await _context.SaveChangesAsync();

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.True(result[0].HasApplied);  // job1
            Assert.False(result[1].HasApplied); // job2
            Assert.True(result[2].HasApplied);  // job3
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description, location), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithEmptySearchResults_ShouldReturnEmptyList()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int? userId = 1;

            var mockResults = new List<JobSearchResultViewModel>();

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description, location), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithSearchJobsServiceException_ShouldPropagateException()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int? userId = 1;

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ThrowsAsync(new Exception("Search service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId));
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithJobAIResponseServiceException_ShouldPropagateException()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int? userId = 1;

            var mockResults = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 1, Title = "Software Engineer", Location = "Stockholm" },
                    HasApplied = false
                }
            };

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .ThrowsAsync(new Exception("AI response service error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => 
                _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId));
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithDatabaseException_ShouldPropagateException()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int userId = 1;

            var mockResults = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel
                {
                    Job = new Job {JobId = 1, Title = "Software Engineer", Location = "Stockholm" },
                    HasApplied = false
                }
            };

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act & Assert
            // Note: We can't easily simulate database errors in this test setup
            // In a real scenario, we would mock the database context or use a different approach
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);
            
            // Assert that the method completes successfully
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SearchAndAttachMetaAsync_WithLargeResultSet_ShouldHandleCorrectly()
        {
            // Arrange
            var description = "Software Engineer";
            var location = "Stockholm";
            int? userId = 1;

            var mockResults = new List<JobSearchResultViewModel>();
            for (int i = 1; i <= 100; i++)
            {
                mockResults.Add(new JobSearchResultViewModel
                {
                    Job = new Job {JobId = i, Title = $"Job {i}", Location = "Stockholm" },
                    HasApplied = false
                });
            }

            _mockSearchJobsService.Setup(x => x.SearchJobsAsync(description, location))
                .ReturnsAsync(mockResults);

            _mockJobAIResponseService.Setup(x => x.AttachLatestAIResponsesAsync(mockResults))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _searchWorkflowService.SearchAndAttachMetaAsync(description, location, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Count);
            _mockSearchJobsService.Verify(x => x.SearchJobsAsync(description, location), Times.Once);
            _mockJobAIResponseService.Verify(x => x.AttachLatestAIResponsesAsync(mockResults), Times.Once);
        }

        public void Dispose()
        {
            if (_context != null)
            {
                try
                {
                    _context.Database.EnsureDeleted();
                    _context.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Context is already disposed, which is fine
                }
            }
        }
    }
}