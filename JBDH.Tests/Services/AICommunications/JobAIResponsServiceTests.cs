
using JBDH.Models.Core;
using JBDH.Models.ViewModels;
using JBDH.Models.AI;
using JBDH.Services.AICommunications;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Tests.Services.AICommunications
{
    public class JobAIResponseServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly JobAIResponseService _service;

        public JobAIResponseServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _service = new JobAIResponseService(_context);
        }

        [Fact]
        public async Task GetLatestJobAIResponseAsync_ReturnsLatestResponse()
        {
            // Arrange
            int jobId = 1;
            var oldResponse = new JobAIResponse
            {
                JobId = jobId,
                ResponseText = "Old",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };
            var latestResponse = new JobAIResponse
            {
                JobId = jobId,
                ResponseText = "Latest",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };
            _context.JobAIResponses.AddRange(oldResponse, latestResponse);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetLatestJobAIResponseAsync(jobId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Latest", result.ResponseText);
        }

        [Fact]
        public async Task AttachLatestAIResponsesAsync_AttachesLatestResponseToJobs()
        {
            // Arrange
            int jobId = 2;
            var jobList = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel
                {
                    Job = new JBDH.Models.Job.Job
                    {
                        JobId = jobId
                    }
                }
            };
            var response = new JobAIResponse
            {
                JobId = jobId,
                ResponseText = "AI Response",
                CreatedAt = DateTime.UtcNow
            };
            _context.JobAIResponses.Add(response);
            await _context.SaveChangesAsync();

            // Act
            await _service.AttachLatestAIResponsesAsync(jobList);

            // Assert
            Assert.NotNull(jobList[0].LatestAIResponse);
            Assert.Equal("AI Response", jobList[0].LatestAIResponse.ResponseText);
        }

        [Fact]
        public async Task AttachLatestAIResponsesAsync_SetsNullIfNoJobOrJobId()
        {
            // Arrange
            var jobList = new List<JobSearchResultViewModel>
            {
                new JobSearchResultViewModel { Job = null },
                new JobSearchResultViewModel { Job = new JBDH.Models.Job.Job { JobId = 0 } }
            };

            // Act
            await _service.AttachLatestAIResponsesAsync(jobList);

            // Assert
            Assert.Null(jobList[0].LatestAIResponse);
            Assert.Null(jobList[1].LatestAIResponse);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}