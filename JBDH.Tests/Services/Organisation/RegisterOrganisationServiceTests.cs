using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.Core;
using JBDH.Models.Organisation;
using JBDH.Models.User;
using JBDH.Services.Organisation;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OrganisationModel = JBDH.Models.Organisation.Organisation;

namespace JBDH.Tests.Services.Organisation
{
    public class RegisterOrganisationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RegisterOrganisationService _registerOrganisationService;
        private readonly User _managerUser;
        private readonly User _regularUser;

        public RegisterOrganisationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _registerOrganisationService = new RegisterOrganisationService(_context);

            // Create test users
            _managerUser = new User
            {
                Id = 1,
                PasswordHash = "hash1",
                Email = "manager1@test.com",
                Location = "Location1"
            };

            _regularUser = new User
            {
                Id = 2,
                PasswordHash = "hash2",
                Email = "user1@test.com",
                Location = "Location2"
            };

            // Add manager role to manager user
            var managerRole = new UserRoleAssignment
            {
                UserId = _managerUser.Id,
                User = _managerUser,
                Role = "Manager"
            };

            _managerUser.Roles.Add(managerRole);

            _context.Users.AddRange(_managerUser, _regularUser);
            _context.UserRoleAssignments.Add(managerRole);
            _context.SaveChanges();
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldCreateOrganisation()
        {
            // Arrange
            var name = "Test Organisation";
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Equal(name, organisation.Name);
        }

        [Fact]
        public async Task RegisterAsync_WithEmptyName_ShouldReturnFailure()
        {
            // Arrange
            var name = "";
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Organisation name cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithNullName_ShouldReturnFailure()
        {
            // Arrange
            string? name = null;
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name!, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Organisation name cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithNonManagerUser_ShouldReturnFailure()
        {
            // Arrange
            var name = "Test Organisation";
            var managerUsername = "user1"; // Regular user, not a manager

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("does not have permission", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithNonExistentUser_ShouldReturnFailure()
        {
            // Arrange
            var name = "Test Organisation";
            var managerUsername = "nonexistent";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Manager user not found", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithWhitespaceName_ShouldReturnFailure()
        {
            // Arrange
            var name = "   ";
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Organisation name cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateName_ShouldReturnFailure()
        {
            // Arrange
            var name = "Test Organisation";
            var managerUsername = "manager1";

            // Create first organisation
            var existingOrganisation = new OrganisationModel
            {
                Name = name
            };
            _context.Organisations.Add(existingOrganisation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already exists", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithLongName_ShouldCreateOrganisation()
        {
            // Arrange
            var name = new string('A', 100); // 100 character name
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Equal(name, organisation.Name);
        }

        [Fact]
        public async Task RegisterAsync_WithSpecialCharacters_ShouldCreateOrganisation()
        {
            // Arrange
            var name = "Test Organisation & Co. (Ltd.)";
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Equal(name, organisation.Name);
        }

        [Fact]
        public async Task RegisterAsync_WithUnicodeCharacters_ShouldCreateOrganisation()
        {
            // Arrange
            var name = "Test Organisation with Unicode: 测试组织";
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Equal(name, organisation.Name);
        }

        [Fact]
        public async Task RegisterAsync_WithMultipleOrganisations_ShouldCreateAllOrganisations()
        {
            // Arrange
            var name1 = "Test Organisation 1";
            var name2 = "Test Organisation 2";
            var managerUsername = "manager1";

            // Act
            var result1 = await _registerOrganisationService.RegisterAsync(name1, managerUsername);
            var result2 = await _registerOrganisationService.RegisterAsync(name2, managerUsername);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            
            var organisations = await _context.Organisations
                .Where(o => o.Name.StartsWith("Test Organisation"))
                .ToListAsync();
            Assert.Equal(2, organisations.Count);
        }

        [Fact]
        public async Task RegisterAsync_WithUserHavingMultipleRoles_ShouldCreateOrganisation()
        {
            // Arrange
            var name = "Test Organisation";
            var managerUsername = "manager1";

            // Add Admin role to manager user (already has Manager role)
            var additionalRole = new UserRoleAssignment
            {
                UserId = _managerUser.Id,
                User = _managerUser,
                Role = "Admin"
            };
            _managerUser.Roles.Add(additionalRole);
            _context.UserRoleAssignments.Add(additionalRole);
            await _context.SaveChangesAsync();

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Equal(name, organisation.Name);
        }

        [Fact]
        public async Task RegisterAsync_WithEmptyManagerUsername_ShouldReturnFailure()
        {
            // Arrange
            var name = "Test Organisation";
            var managerUsername = "";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Manager user not found", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithNullManagerUsername_ShouldReturnFailure()
        {
            // Arrange
            var name = "Test Organisation";
            string? managerUsername = null;

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername!);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Manager user not found", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WithCaseSensitiveName_ShouldCreateOrganisation()
        {
            // Arrange
            var name = "Test ORGANISATION";
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Equal(name, organisation.Name);
        }

        [Fact]
        public async Task RegisterAsync_WithNumbersInName_ShouldCreateOrganisation()
        {
            // Arrange
            var name = "Test Organisation 123";
            var managerUsername = "manager1";

            // Act
            var result = await _registerOrganisationService.RegisterAsync(name, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Equal(name, organisation.Name);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}