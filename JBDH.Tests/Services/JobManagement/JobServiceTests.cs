using Microsoft.EntityFrameworkCore;
using JBDH.Models.Core;
using JBDH.Models.Job;
using JBDH.Models.User;
using JBDH.Services.JobManagement;
using JBDH.Models.ViewModels;

namespace JBDH.Tests.Services.JobManagement
{
    public class JobServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly JobService _jobService;

        public JobServiceTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _jobService = new JobService(_context);
        }

        [Fact]
        public async Task GetJobDetailsAsync_WithValidJobId_ShouldReturnJobDetails()
        {
            // Arrange
            var job = new Job
            {
                JobId = 1,
                Title = "Software Developer",
                Description = "Develop software applications",
                Company = "Tech Corp",
                Location = "Stockholm",
                PostedDate = DateTime.UtcNow.AddDays(-5),
                WebpageUrl = "https://example.com/job"
            };
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jobService.GetJobDetailsAsync(1, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(job, result.Job);
            Assert.False(result.HasApplied);
        }

        [Fact]
        public async Task GetJobDetailsAsync_WithValidJobIdAndUser_ShouldReturnJobDetailsWithApplicationStatus()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Location = "Test Location"
            };
            await _context.Users.AddAsync(user);

            var job = new Job
            {
                JobId = 1,
                Title = "Software Developer",
                Description = "Develop software applications",
                Company = "Tech Corp",
                Location = "Stockholm",
                PostedDate = DateTime.UtcNow.AddDays(-5),
                WebpageUrl = "https://example.com/job"
            };
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jobService.GetJobDetailsAsync(1, user.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(job, result.Job);
            Assert.False(result.HasApplied);
        }

        [Fact]
        public async Task GetJobDetailsAsync_WithUserWhoHasApplied_ShouldReturnHasAppliedTrue()
        {
            // Arrange
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Location = "Test Location"
            };
            await _context.Users.AddAsync(user);

            var job = new Job
            {
                JobId = 1,
                Title = "Software Developer",
                Description = "Develop software applications",
                Company = "Tech Corp",
                Location = "Stockholm",
                PostedDate = DateTime.UtcNow.AddDays(-5),
                WebpageUrl = "https://example.com/job"
            };
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();

            var application = new JobApplication
            {
                UserId = user.Id,
                JobId = 1,
                AppliedAt = DateTime.UtcNow,
                Notes = "I'm interested in this position"
            };
            await _context.JobApplications.AddAsync(application);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jobService.GetJobDetailsAsync(1, user.Email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(job, result.Job);
            Assert.True(result.HasApplied);
        }

        [Fact]
        public async Task GetJobDetailsAsync_WithInvalidJobId_ShouldReturnNull()
        {
            // Arrange
            var invalidJobId = 999;

            // Act
            var result = await _jobService.GetJobDetailsAsync(invalidJobId, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetJobDetailsAsync_WithInvalidUsername_ShouldReturnJobDetailsWithoutApplication()
        {
            // Arrange
            var job = new Job
            {
                JobId = 1,
                Title = "Software Developer",
                Description = "Develop software applications",
                Company = "Tech Corp",
                Location = "Stockholm",
                PostedDate = DateTime.UtcNow.AddDays(-5),
                WebpageUrl = "https://example.com/job"
            };
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jobService.GetJobDetailsAsync(1, "nonexistentuser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(job, result.Job);
            Assert.False(result.HasApplied);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}