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
    public class OrganisationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OrganisationService _organisationService;
        private readonly User _managerUser;
        private readonly User _regularUser;

        public OrganisationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _organisationService = new OrganisationService(_context);

            // Create test users
            _managerUser = new User
            {
                Id = 1,
                Username = "manager1",
                PasswordHash = "hash1",
                Email = "manager1@test.com",
                Location = "Location1"
            };

            _regularUser = new User
            {
                Id = 2,
                Username = "user1",
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
        public async Task RegisterOrganisationAsync_WithValidData_ShouldCreateOrganisation()
        {
            // Arrange
            var name = "Test Organisation";
            var description = "Test Description";
            var managerUsername = "manager1";

            // Act
            var result = await _organisationService.RegisterOrganisationAsync(name, description, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Single(organisation.Managers);
            Assert.Contains(organisation.Managers, m => m.Username == managerUsername);
        }

        [Fact]
        public async Task RegisterOrganisationAsync_WithEmptyName_ShouldReturnFailure()
        {
            // Arrange
            var name = "";
            var description = "Test Description";
            var managerUsername = "manager1";

            // Act
            var result = await _organisationService.RegisterOrganisationAsync(name, description, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Organisation name is required", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterOrganisationAsync_WithNullName_ShouldReturnFailure()
        {
            // Arrange
            string? name = null;
            var description = "Test Description";
            var managerUsername = "manager1";

            // Act
            var result = await _organisationService.RegisterOrganisationAsync(name!, description, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Organisation name is required", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterOrganisationAsync_WithNonManagerUser_ShouldReturnFailure()
        {
            // Arrange
            var name = "Test Organisation";
            var description = "Test Description";
            var managerUsername = "user1"; // Regular user, not a manager

            // Act
            var result = await _organisationService.RegisterOrganisationAsync(name, description, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("must be a manager", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterOrganisationAsync_WithNonExistentUser_ShouldReturnFailure()
        {
            // Arrange
            var name = "Test Organisation";
            var description = "Test Description";
            var managerUsername = "nonexistent";

            // Act
            var result = await _organisationService.RegisterOrganisationAsync(name, description, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("must be a manager", result.ErrorMessage);
        }

        [Fact]
        public async Task AddManagerAsync_WithValidData_ShouldAddManager()
        {
            // Arrange
            var organisation = new OrganisationModel
            {
                Name = "Test Organisation"
            };
            _context.Organisations.Add(organisation);
            await _context.SaveChangesAsync();

            var managerUsername = "manager1";

            // Act
            var result = await _organisationService.AddManagerAsync(organisation.Id, managerUsername);

            // Assert
            Assert.True(result.Success);
            var updatedOrganisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Id == organisation.Id);
            Assert.NotNull(updatedOrganisation);
            Assert.Single(updatedOrganisation.Managers);
            Assert.Contains(updatedOrganisation.Managers, m => m.Username == managerUsername);
        }

        [Fact]
        public async Task AddManagerAsync_WithEmptyManagerUsername_ShouldReturnFailure()
        {
            // Arrange
            var organisation = new OrganisationModel
            {
                Name = "Test Organisation"
            };
            _context.Organisations.Add(organisation);
            await _context.SaveChangesAsync();

            var managerUsername = "";

            // Act
            var result = await _organisationService.AddManagerAsync(organisation.Id, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Manager username is required", result.ErrorMessage);
        }

        [Fact]
        public async Task AddManagerAsync_WithNullManagerUsername_ShouldReturnFailure()
        {
            // Arrange
            var organisation = new OrganisationModel
            {
                Name = "Test Organisation"
            };
            _context.Organisations.Add(organisation);
            await _context.SaveChangesAsync();

            string? managerUsername = null;

            // Act
            var result = await _organisationService.AddManagerAsync(organisation.Id, managerUsername!);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Manager username is required", result.ErrorMessage);
        }

        [Fact]
        public async Task AddManagerAsync_WithNonExistentOrganisation_ShouldReturnFailure()
        {
            // Arrange
            var managerUsername = "manager1";

            // Act
            var result = await _organisationService.AddManagerAsync(999, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Organisation not found", result.ErrorMessage);
        }

        [Fact]
        public async Task AddManagerAsync_WithNonManagerUser_ShouldReturnFailure()
        {
            // Arrange
            var organisation = new OrganisationModel
            {
                Name = "Test Organisation"
            };
            _context.Organisations.Add(organisation);
            await _context.SaveChangesAsync();

            var managerUsername = "user1"; // Regular user, not a manager

            // Act
            var result = await _organisationService.AddManagerAsync(organisation.Id, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Manager not found", result.ErrorMessage);
        }

        [Fact]
        public async Task AddManagerAsync_WithNonExistentUser_ShouldReturnFailure()
        {
            // Arrange
            var organisation = new OrganisationModel
            {
                Name = "Test Organisation"
            };
            _context.Organisations.Add(organisation);
            await _context.SaveChangesAsync();

            var managerUsername = "nonexistent";

            // Act
            var result = await _organisationService.AddManagerAsync(organisation.Id, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Manager not found", result.ErrorMessage);
        }

        [Fact]
        public async Task AddManagerAsync_WithAlreadyAddedManager_ShouldReturnFailure()
        {
            // Arrange
            var organisation = new OrganisationModel
            {
                Name = "Test Organisation",
                Managers = new List<User> { _managerUser }
            };
            _context.Organisations.Add(organisation);
            await _context.SaveChangesAsync();

            var managerUsername = "manager1";

            // Act
            var result = await _organisationService.AddManagerAsync(organisation.Id, managerUsername);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("already part of the organisation", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterOrganisationAsync_WithMultipleManagers_ShouldCreateOrganisationWithOneManager()
        {
            // Arrange
            var name = "Test Organisation";
            var description = "Test Description";
            var managerUsername = "manager1";

            // Act
            var result = await _organisationService.RegisterOrganisationAsync(name, description, managerUsername);

            // Assert
            Assert.True(result.Success);
            var organisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Name == name);
            Assert.NotNull(organisation);
            Assert.Single(organisation.Managers);
        }

        [Fact]
        public async Task AddManagerAsync_WithMultipleManagers_ShouldAddSecondManager()
        {
            // Arrange
            var organisation = new OrganisationModel
            {
                Name = "Test Organisation",
                Managers = new List<User> { _managerUser }
            };
            _context.Organisations.Add(organisation);
            await _context.SaveChangesAsync();

            // Create second manager
            var secondManager = new User
            {
                Id = 3,
                Username = "manager2",
                PasswordHash = "hash3",
                Email = "manager2@test.com",
                Location = "Location3"
            };
            var secondManagerRole = new UserRoleAssignment
            {
                UserId = secondManager.Id,
                User = secondManager,
                Role = "Manager"
            };
            secondManager.Roles.Add(secondManagerRole);
            _context.Users.Add(secondManager);
            _context.UserRoleAssignments.Add(secondManagerRole);
            await _context.SaveChangesAsync();

            // Act
            var result = await _organisationService.AddManagerAsync(organisation.Id, "manager2");

            // Assert
            Assert.True(result.Success);
            var updatedOrganisation = await _context.Organisations
                .Include(o => o.Managers)
                .FirstOrDefaultAsync(o => o.Id == organisation.Id);
            Assert.NotNull(updatedOrganisation);
            Assert.Equal(2, updatedOrganisation.Managers.Count);
            Assert.Contains(updatedOrganisation.Managers, m => m.Username == "manager1");
            Assert.Contains(updatedOrganisation.Managers, m => m.Username == "manager2");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 