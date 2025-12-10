/*using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using JBDH.Models.Core;
using JBDH.Models.Job;
using JBDH.Models.ViewModels;
using JBDH.Services.JobManagement;
using JBDH.Services.Core;

namespace JBDH.Tests.Services.JobManagement
{
    public class SearchJobsServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly SearchJobsService _searchJobsService;

        public SearchJobsServiceTests()
        {
            // Set up in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Set up environment mocking
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

            _searchJobsService = new SearchJobsService(_context, _mockEnvironment.Object);
        }

        [Fact]
        public void SearchJobsService_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var service = new SearchJobsService(_context, _mockEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void SearchJobsService_WithValidEnvironment_ShouldBeCreated()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

            // Act
            var service = new SearchJobsService(_context, _mockEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void SearchJobsService_WithProductionEnvironment_ShouldBeCreated()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            var service = new SearchJobsService(_context, _mockEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void SearchJobsService_WithTestEnvironment_ShouldBeCreated()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Test");

            // Act
            var service = new SearchJobsService(_context, _mockEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void SearchJobsService_WithNullEnvironment_ShouldBeCreated()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(string.Empty);

            // Act
            var service = new SearchJobsService(_context, _mockEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void SearchJobsService_WithEmptyEnvironment_ShouldBeCreated()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("");

            // Act
            var service = new SearchJobsService(_context, _mockEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void SearchJobsService_WithInMemoryDatabase_ShouldBeCreated()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var testContext = new ApplicationDbContext(options);

            // Act
            var service = new SearchJobsService(testContext, _mockEnvironment.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void SearchJobsService_WithMultipleInstances_ShouldBeIndependent()
        {
            // Arrange
            var service1 = new SearchJobsService(_context, _mockEnvironment.Object);
            var service2 = new SearchJobsService(_context, _mockEnvironment.Object);

            // Act & Assert
            Assert.NotNull(service1);
            Assert.NotNull(service2);
            Assert.NotSame(service1, service2);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 

*/