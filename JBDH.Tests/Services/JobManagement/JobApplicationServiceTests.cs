using Microsoft.EntityFrameworkCore;
using JBDH.Models.Core;
using JBDH.Models.Job;
using JBDH.Models.User;
using JBDH.Models.Services;
using JBDH.Services.JobManagement;

namespace JBDH.Tests.Services.JobManagement
{
    public class JobApplicationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly JobApplicationService _jobApplicationService;

        public JobApplicationServiceTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _jobApplicationService = new JobApplicationService(_context);
        }

        [Fact]
        public async Task HasUserAppliedAsync_WithNoApplication_ShouldReturnFalse()
        {
            // Arrange
            var userId = 1;
            var jobId = 1;

            // Act
            var result = await _jobApplicationService.HasUserAppliedAsync(userId, jobId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasUserAppliedAsync_WithExistingApplication_ShouldReturnTrue()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
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
                Location = "Stockholm"
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
            var result = await _jobApplicationService.HasUserAppliedAsync(user.Id, 1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ApplyJobAsync_WithValidApplication_ShouldCreateApplicationAndReturnSuccess()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
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
                Location = "Stockholm"
            };
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();

            var notes = "I'm very interested in this position and have relevant experience.";

            // Act
            var result = await _jobApplicationService.ApplyJobAsync(user.Id, 1, notes);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            var application = await _context.JobApplications.FirstOrDefaultAsync(ja => ja.UserId == user.Id && ja.JobId == 1);
            Assert.NotNull(application);
            Assert.Equal(user.Id, application.UserId);
            Assert.Equal(1, application.JobId);
            Assert.Equal(notes, application.Notes);
            Assert.True(application.AppliedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task ApplyJobAsync_WithDuplicateApplication_ShouldReturnFailure()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
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
                Location = "Stockholm"
            };
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();

            var application = new JobApplication
            {
                UserId = user.Id,
                JobId = 1,
                AppliedAt = DateTime.UtcNow,
                Notes = "First application"
            };
            await _context.JobApplications.AddAsync(application);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jobApplicationService.ApplyJobAsync(user.Id, 1, "Second application attempt");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User has already applied for this job.", result.ErrorMessage);

            var applications = await _context.JobApplications.Where(ja => ja.UserId == user.Id && ja.JobId == 1).ToListAsync();
            Assert.Single(applications); // Should still only have one application
        }

        [Fact]
        public async Task ApplyJobAsync_WithEmptyNotes_ShouldCreateApplicationSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
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
                Location = "Stockholm"
            };
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jobApplicationService.ApplyJobAsync(user.Id, 1, "");

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            var application = await _context.JobApplications.FirstOrDefaultAsync(ja => ja.UserId == user.Id && ja.JobId == 1);
            Assert.NotNull(application);
            Assert.Equal("", application.Notes);
        }

        [Fact]
        public async Task UnapplyJobAsync_WithExistingApplication_ShouldRemoveApplicationAndReturnSuccess()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
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
                Location = "Stockholm"
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
            var result = await _jobApplicationService.UnapplyJobAsync(user.Id, 1);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            var remainingApplication = await _context.JobApplications.FirstOrDefaultAsync(ja => ja.UserId == user.Id && ja.JobId == 1);
            Assert.Null(remainingApplication);
        }

        [Fact]
        public async Task UnapplyJobAsync_WithNoExistingApplication_ShouldReturnFailure()
        {
            // Arrange
            var userId = 1;
            var jobId = 1;

            // Act
            var result = await _jobApplicationService.UnapplyJobAsync(userId, jobId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("No application found to remove.", result.ErrorMessage);
        }

        [Fact]
        public async Task UnapplyJobAsync_WithDifferentUserApplication_ShouldReturnFailure()
        {
            // Arrange
            var user1 = new User
            {
                Username = "user1",
                Email = "user1@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Location = "Test Location"
            };
            var user2 = new User
            {
                Username = "user2",
                Email = "user2@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
                Location = "Test Location"
            };
            await _context.Users.AddRangeAsync(user1, user2);

            var job = new Job
            {
                JobId = 1,
                Title = "Software Developer",
                Description = "Develop software applications",
                Company = "Tech Corp",
                Location = "Stockholm"
            };
            await _context.Jobs.AddAsync(job);
            await _context.SaveChangesAsync();

            var application = new JobApplication
            {
                UserId = user1.Id,
                JobId = 1,
                AppliedAt = DateTime.UtcNow,
                Notes = "I'm interested in this position"
            };
            await _context.JobApplications.AddAsync(application);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jobApplicationService.UnapplyJobAsync(user2.Id, 1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("No application found to remove.", result.ErrorMessage);

            var remainingApplication = await _context.JobApplications.FirstOrDefaultAsync(ja => ja.UserId == user1.Id && ja.JobId == 1);
            Assert.NotNull(remainingApplication); // User1's application should still exist
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 