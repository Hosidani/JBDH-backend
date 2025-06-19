using System;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JBDH.Tests.Services.Reporting
{
    public class RapportUserServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RapportUserService _rapportUserService;
        private readonly User _reporterUser;
        private readonly User _reportedUser;

        public RapportUserServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _rapportUserService = new RapportUserService(_context);

            // Create test users
            _reporterUser = new User
            {
                Id = 1,
                Username = "reporter",
                Email = "reporter@test.com",
                PasswordHash = "hash1",
                Location = "Stockholm"
            };

            _reportedUser = new User
            {
                Id = 2,
                Username = "reported",
                Email = "reported@test.com",
                PasswordHash = "hash2",
                Location = "Gothenburg"
            };

            _context.Users.AddRange(_reporterUser, _reportedUser);
            _context.SaveChanges();
        }

        [Fact]
        public async Task TryReportUserAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason = "Inappropriate behavior";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            // Verify report was saved
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId);
            
            Assert.NotNull(savedReport);
            Assert.Equal(reason, savedReport.Reason);
            Assert.Equal(reporterUserId, savedReport.ReporterUserId);
            Assert.Equal(reportedUserId, savedReport.ReportedUserId);
            Assert.Null(savedReport.DataFlowId);
            Assert.Equal(string.Empty, savedReport.DataFlowText);
        }

        [Fact]
        public async Task TryReportUserAsync_WithDataFlowContext_ShouldSaveDataFlowInfo()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason = "Inappropriate comment";
            var dataFlowId = 123;
            var dataFlowText = "This is the offensive comment";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, dataFlowId, dataFlowText);

            // Assert
            Assert.True(result.Success);

            // Verify report was saved with data flow info
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId);
            
            Assert.NotNull(savedReport);
            Assert.Equal(dataFlowId, savedReport.DataFlowId);
            Assert.Equal(dataFlowText, savedReport.DataFlowText);
        }

        [Fact]
        public async Task TryReportUserAsync_WithEmptyReason_ShouldReturnFailure()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason = "";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reason cannot be empty.", result.ErrorMessage);

            // Verify no report was saved
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId);
            
            Assert.Null(savedReport);
        }

        [Fact]
        public async Task TryReportUserAsync_WithWhitespaceReason_ShouldReturnFailure()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason = "   ";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reason cannot be empty.", result.ErrorMessage);
        }

        [Fact]
        public async Task TryReportUserAsync_WithNullReason_ShouldReturnFailure()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            string? reason = null;

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason!, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reason cannot be empty.", result.ErrorMessage);
        }

        [Fact]
        public async Task TryReportUserAsync_WithNonExistentReporter_ShouldReturnFailure()
        {
            // Arrange
            var reporterUserId = 999; // Non-existent
            var reportedUserId = 2;
            var reason = "Valid reason";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reporter or reported user does not exist.", result.ErrorMessage);

            // Verify no report was saved
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId);
            
            Assert.Null(savedReport);
        }

        [Fact]
        public async Task TryReportUserAsync_WithNonExistentReportedUser_ShouldReturnFailure()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 999; // Non-existent
            var reason = "Valid reason";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reporter or reported user does not exist.", result.ErrorMessage);
        }

        [Fact]
        public async Task TryReportUserAsync_WithBothNonExistentUsers_ShouldReturnFailure()
        {
            // Arrange
            var reporterUserId = 999; // Non-existent
            var reportedUserId = 888; // Non-existent
            var reason = "Valid reason";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reporter or reported user does not exist.", result.ErrorMessage);
        }

        [Fact]
        public async Task TryReportUserAsync_WithNullDataFlowText_ShouldUseEmptyString()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason = "Valid reason";
            var dataFlowId = 123;
            string? dataFlowText = null;

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, dataFlowId, dataFlowText);

            // Assert
            Assert.True(result.Success);

            // Verify report was saved with empty string for data flow text
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId);
            
            Assert.NotNull(savedReport);
            Assert.Equal(string.Empty, savedReport.DataFlowText);
        }

        [Fact]
        public async Task TryReportUserAsync_WithLongReason_ShouldSucceed()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason = new string('A', 1000); // Very long reason

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.True(result.Success);

            // Verify report was saved with long reason
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId);
            
            Assert.NotNull(savedReport);
            Assert.Equal(reason, savedReport.Reason);
        }

        [Fact]
        public async Task TryReportUserAsync_WithSpecialCharactersInReason_ShouldSucceed()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.True(result.Success);

            // Verify report was saved with special characters
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId);
            
            Assert.NotNull(savedReport);
            Assert.Equal(reason, savedReport.Reason);
        }

        [Fact]
        public async Task TryReportUserAsync_WithUnicodeCharacters_ShouldSucceed()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason = "Unicode: åäöéèêñüß";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.True(result.Success);

            // Verify report was saved with unicode characters
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId);
            
            Assert.NotNull(savedReport);
            Assert.Equal(reason, savedReport.Reason);
        }

        [Fact]
        public async Task TryReportUserAsync_WithSameUserReportingThemselves_ShouldSucceed()
        {
            // Arrange
            var userId = 1;
            var reason = "Self-report for testing";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(userId, userId, reason, null, null);

            // Assert
            Assert.True(result.Success);

            // Verify report was saved
            var savedReport = _context.UserReports.FirstOrDefault(r => 
                r.ReporterUserId == userId && 
                r.ReportedUserId == userId);
            
            Assert.NotNull(savedReport);
        }

        [Fact]
        public async Task TryReportUserAsync_WithNegativeUserIds_ShouldReturnFailure()
        {
            // Arrange
            var reporterUserId = -1;
            var reportedUserId = -2;
            var reason = "Valid reason";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reporter or reported user does not exist.", result.ErrorMessage);
        }

        [Fact]
        public async Task TryReportUserAsync_WithZeroUserIds_ShouldReturnFailure()
        {
            // Arrange
            var reporterUserId = 0;
            var reportedUserId = 0;
            var reason = "Valid reason";

            // Act
            var result = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason, null, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Reporter or reported user does not exist.", result.ErrorMessage);
        }

        [Fact]
        public async Task TryReportUserAsync_WithMultipleReports_ShouldSaveAllReports()
        {
            // Arrange
            var reporterUserId = 1;
            var reportedUserId = 2;
            var reason1 = "First report";
            var reason2 = "Second report";

            // Act
            var result1 = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason1, null, null);
            var result2 = await _rapportUserService.TryReportUserAsync(reporterUserId, reportedUserId, reason2, null, null);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);

            // Verify both reports were saved
            var savedReports = _context.UserReports.Where(r => 
                r.ReporterUserId == reporterUserId && 
                r.ReportedUserId == reportedUserId).ToList();
            
            Assert.Equal(2, savedReports.Count);
            Assert.Contains(savedReports, r => r.Reason == reason1);
            Assert.Contains(savedReports, r => r.Reason == reason2);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
} 