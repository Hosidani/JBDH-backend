using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.User;
using JBDH.Services.UserManagement;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JBDH.Tests.Services.UserManagement
{
    public class UserRoleServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRoleService _userRoleService;
        private readonly User _user1;
        private readonly User _user2;

        public UserRoleServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _userRoleService = new UserRoleService(_context);

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

            _context.Users.AddRange(_user1, _user2);
            _context.SaveChanges();
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithValidUserAndRoles_ShouldUpdateRoles()
        {
            // Arrange
            var selectedRoles = new List<string> { "Manager", "Recruiter" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Equal(3, userRoles.Count); // User + Manager + Recruiter
            Assert.Contains(userRoles, r => r.Role == "User");
            Assert.Contains(userRoles, r => r.Role == "Manager");
            Assert.Contains(userRoles, r => r.Role == "Recruiter");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Arrange
            var selectedRoles = new List<string> { "Manager" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(999, selectedRoles);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithEmptyRoles_ShouldKeepOnlyUserRole()
        {
            // Arrange
            var selectedRoles = new List<string>();

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Single(userRoles);
            Assert.Contains(userRoles, r => r.Role == "User");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithUserRoleInSelectedRoles_ShouldNotDuplicate()
        {
            // Arrange
            var selectedRoles = new List<string> { "User", "Manager" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Equal(2, userRoles.Count); // User + Manager (no duplicate)
            Assert.Contains(userRoles, r => r.Role == "User");
            Assert.Contains(userRoles, r => r.Role == "Manager");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithDuplicateRoles_ShouldAssignUniqueRoles()
        {
            // Arrange
            var selectedRoles = new List<string> { "Manager", "Manager", "Recruiter" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Equal(3, userRoles.Count); // User + Manager + Recruiter (no duplicates)
            Assert.Contains(userRoles, r => r.Role == "User");
            Assert.Contains(userRoles, r => r.Role == "Manager");
            Assert.Contains(userRoles, r => r.Role == "Recruiter");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithExistingRoles_ShouldReplaceRoles()
        {
            // Arrange - Add initial roles
            var initialRoles = new List<UserRoleAssignment>
            {
                new UserRoleAssignment { UserId = _user1.Id, User = _user1, Role = "Manager" },
                new UserRoleAssignment { UserId = _user1.Id, User = _user1, Role = "Recruiter" }
            };
            _context.UserRoleAssignments.AddRange(initialRoles);
            await _context.SaveChangesAsync();

            var newRoles = new List<string> { "Admin" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, newRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Equal(2, userRoles.Count); // User + Admin
            Assert.Contains(userRoles, r => r.Role == "User");
            Assert.Contains(userRoles, r => r.Role == "Admin");
            Assert.DoesNotContain(userRoles, r => r.Role == "Manager");
            Assert.DoesNotContain(userRoles, r => r.Role == "Recruiter");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithNullRoles_ShouldKeepOnlyUserRole()
        {
            // Arrange
            List<string>? selectedRoles = null;

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles!);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Single(userRoles);
            Assert.Contains(userRoles, r => r.Role == "User");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithZeroUserId_ShouldReturnFalse()
        {
            // Arrange
            var selectedRoles = new List<string> { "Manager" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(0, selectedRoles);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithNegativeUserId_ShouldReturnFalse()
        {
            // Arrange
            var selectedRoles = new List<string> { "Manager" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(-1, selectedRoles);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithMultipleUsers_ShouldUpdateCorrectUser()
        {
            // Arrange
            var selectedRoles = new List<string> { "Manager" };

            // Act
            var result1 = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);
            var result2 = await _userRoleService.UpdateUserRolesAsync(_user2.Id, new List<string> { "Recruiter" });

            // Assert
            Assert.True(result1);
            Assert.True(result2);

            var user1Roles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            var user2Roles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user2.Id)
                .ToListAsync();

            Assert.Equal(2, user1Roles.Count); // User + Manager
            Assert.Equal(2, user2Roles.Count); // User + Recruiter
            Assert.Contains(user1Roles, r => r.Role == "Manager");
            Assert.Contains(user2Roles, r => r.Role == "Recruiter");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithSpecialCharactersInRole_ShouldAssignRole()
        {
            // Arrange
            var selectedRoles = new List<string> { "Special-Role_123" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Equal(2, userRoles.Count); // User + Special-Role_123
            Assert.Contains(userRoles, r => r.Role == "User");
            Assert.Contains(userRoles, r => r.Role == "Special-Role_123");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithEmptyStringRole_ShouldAssignRole()
        {
            // Arrange
            var selectedRoles = new List<string> { "" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Equal(2, userRoles.Count); // User + empty string
            Assert.Contains(userRoles, r => r.Role == "User");
            Assert.Contains(userRoles, r => r.Role == "");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithWhitespaceRole_ShouldAssignRole()
        {
            // Arrange
            var selectedRoles = new List<string> { "   " };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Equal(2, userRoles.Count); // User + whitespace
            Assert.Contains(userRoles, r => r.Role == "User");
            Assert.Contains(userRoles, r => r.Role == "   ");
        }

        [Fact]
        public async Task UpdateUserRolesAsync_WithCaseSensitiveRoles_ShouldPreserveCase()
        {
            // Arrange
            var selectedRoles = new List<string> { "MANAGER", "recruiter", "Admin" };

            // Act
            var result = await _userRoleService.UpdateUserRolesAsync(_user1.Id, selectedRoles);

            // Assert
            Assert.True(result);
            var userRoles = await _context.UserRoleAssignments
                .Where(ura => ura.UserId == _user1.Id)
                .ToListAsync();
            Assert.Equal(4, userRoles.Count); // User + MANAGER + recruiter + Admin
            Assert.Contains(userRoles, r => r.Role == "User");
            Assert.Contains(userRoles, r => r.Role == "MANAGER");
            Assert.Contains(userRoles, r => r.Role == "recruiter");
            Assert.Contains(userRoles, r => r.Role == "Admin");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 