using System;
using JBDH.Services.Core;
using Xunit;

namespace JBDH.Tests.Services.Core
{
    public class SettingsServiceTests
    {
        [Fact]
        public void SetupConnection_ShouldReturnValidConnectionString()
        {
            // Act
            var connectionString = SettingsService.SetupConnection();

            // Assert
            Assert.NotNull(connectionString);
            Assert.Contains("Server=127.0.0.1", connectionString);
            Assert.Contains("Database=testing_purposes", connectionString);
            Assert.Contains("User ID=root", connectionString);
            Assert.Contains("Port=3306", connectionString);
        }

        [Fact]
        public void ConnectionString_ShouldBeAccessible()
        {
            // Act
            var connectionString = SettingsService.ConnectionString;

            // Assert
            Assert.NotNull(connectionString);
            Assert.Contains("Server=127.0.0.1", connectionString);
            Assert.Contains("Database=testing_purposes", connectionString);
        }

        [Fact]
        public void ConnectionString_ShouldBeConsistent()
        {
            // Act
            var connectionString1 = SettingsService.ConnectionString;
            var connectionString2 = SettingsService.SetupConnection();

            // Assert
            Assert.Equal(connectionString1, connectionString2);
        }
    }
}