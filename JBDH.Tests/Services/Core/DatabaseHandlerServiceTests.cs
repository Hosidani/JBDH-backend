using System;
using System.IO;
using JBDH.Services.Core;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JBDH.Tests.Services.Core
{
    public class DatabaseHandlerServiceTests
    {
        private const string TestJsonFile = "test_jobs.json";
        private const string TestJsonFilePath = "test_jobs.json";

        [Fact]
        public void SqlQueries_SqlQuery_WithValidQueryId_ShouldReturnQuery()
        {
            // Act
            var createDatabaseQuery = SqlQueries.SqlQuery(0);
            var createUsersTableQuery = SqlQueries.SqlQuery(4);
            var insertRawTableQuery = SqlQueries.SqlQuery(100);

            // Assert
            Assert.NotNull(createDatabaseQuery);
            Assert.Contains("CREATE DATABASE IF NOT EXISTS testing_purposes", createDatabaseQuery);

            Assert.NotNull(createUsersTableQuery);
            Assert.Contains("CREATE TABLE IF NOT EXISTS Users", createUsersTableQuery);
            Assert.Contains("Id INT PRIMARY KEY AUTO_INCREMENT", createUsersTableQuery);

            Assert.NotNull(insertRawTableQuery);
            Assert.Contains("INSERT INTO raw_table", insertRawTableQuery);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithInvalidQueryId_ShouldReturnNull()
        {
            // Act
            var result = SqlQueries.SqlQuery(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithNegativeQueryId_ShouldReturnNull()
        {
            // Act
            var result = SqlQueries.SqlQuery(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithZeroQueryId_ShouldReturnCreateDatabaseQuery()
        {
            // Act
            var result = SqlQueries.SqlQuery(0);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("CREATE DATABASE IF NOT EXISTS testing_purposes", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithTableCreationQueryIds_ShouldReturnValidQueries()
        {
            // Test all table creation queries (0-14)
            for (int i = 0; i <= 14; i++)
            {
                // Act
                var result = SqlQueries.SqlQuery(i);

                // Assert
                Assert.NotNull(result);
                if (i == 0)
                {
                    Assert.Contains("CREATE DATABASE", result);
                }
                else
                {
                    Assert.Contains("CREATE TABLE IF NOT EXISTS", result);
                }
            }
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithInsertQueryIds_ShouldReturnValidQueries()
        {
            // Test insert queries (100-107)
            for (int i = 100; i <= 107; i++)
            {
                // Act
                var result = SqlQueries.SqlQuery(i);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Contains("INSERT") || result.Contains("SELECT"), 
                    $"Query {i} should contain INSERT or SELECT");
            }
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithDeleteQueryIds_ShouldReturnValidQueries()
        {
            // Test delete queries (200-206)
            for (int i = 200; i <= 206; i++)
            {
                // Act
                var result = SqlQueries.SqlQuery(i);

                // Assert
                Assert.NotNull(result);
                Assert.Contains("DELETE FROM", result);
            }
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithUsersTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(4);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Id INT PRIMARY KEY AUTO_INCREMENT", result);
            // Username column removed
            Assert.Contains("PasswordHash VARCHAR(255) NOT NULL", result);
            Assert.Contains("Email VARCHAR(255) NOT NULL UNIQUE", result);
            Assert.Contains("Location VARCHAR(255)", result);
            Assert.Contains("CVPath VARCHAR(2083)", result);
            Assert.Contains("Organisation VARCHAR(255)", result);
            Assert.Contains("DeveloperMode BOOLEAN DEFAULT FALSE", result);
            Assert.Contains("Open BOOLEAN DEFAULT FALSE", result);
            Assert.Contains("Role VARCHAR(100) NOT NULL", result);
            Assert.Contains("IsBanned BOOLEAN DEFAULT FALSE", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithJobsTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(2);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("JobId INT PRIMARY KEY AUTO_INCREMENT", result);
            Assert.Contains("Title VARCHAR(255)", result);
            Assert.Contains("Description TEXT", result);
            Assert.Contains("Company VARCHAR(255)", result);
            Assert.Contains("Location VARCHAR(255)", result);
            Assert.Contains("PostedDate DATE", result);
            Assert.Contains("WebpageUrl VARCHAR(2083)", result);
            Assert.Contains("RecruiterId INT NULL", result);
            Assert.Contains("FOREIGN KEY (RecruiterId) REFERENCES Users(Id)", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithFriendshipsTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(5);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Id INT PRIMARY KEY AUTO_INCREMENT", result);
            Assert.Contains("RequesterId INT NOT NULL", result);
            Assert.Contains("AddresseeId INT NOT NULL", result);
            Assert.Contains("Status INT NOT NULL", result);
            Assert.Contains("RequestedAt DATETIME DEFAULT CURRENT_TIMESTAMP", result);
            Assert.Contains("FOREIGN KEY (RequesterId) REFERENCES Users(Id)", result);
            Assert.Contains("FOREIGN KEY (AddresseeId) REFERENCES Users(Id)", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithJobApplicationsTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(8);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("ApplicationId INT PRIMARY KEY AUTO_INCREMENT", result);
            Assert.Contains("UserId INT NOT NULL", result);
            Assert.Contains("JobId INT NOT NULL", result);
            Assert.Contains("AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP", result);
            Assert.Contains("Notes TEXT", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithDataFlowsTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(9);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Id INT PRIMARY KEY AUTO_INCREMENT", result);
            Assert.Contains("ContentType VARCHAR(50)", result);
            Assert.Contains("Content TEXT", result);
            Assert.Contains("CreatedAt DATETIME", result);
            Assert.Contains("UserId INT", result);
            Assert.Contains("ParentDataFlowId INT NULL", result);
            Assert.Contains("FOREIGN KEY (ParentDataFlowId) REFERENCES DataFlows(Id)", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithUserRoleAssignmentsTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(11);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("UserId INT NOT NULL", result);
            Assert.Contains("Role VARCHAR(50) NOT NULL", result);
            Assert.Contains("PRIMARY KEY (UserId, Role)", result);
            Assert.Contains("FOREIGN KEY (UserId) REFERENCES Users(Id)", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithUserReportsTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(12);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Id INT PRIMARY KEY AUTO_INCREMENT", result);
            Assert.Contains("ReporterUserId INT NOT NULL", result);
            Assert.Contains("ReportedUserId INT NOT NULL", result);
            Assert.Contains("DataFlowId INT NULL", result);
            Assert.Contains("DataFlowText TEXT NULL", result);
            Assert.Contains("Reason TEXT NOT NULL", result);
            Assert.Contains("CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP", result);
            Assert.Contains("FOREIGN KEY (ReportedUserId) REFERENCES Users(Id)", result);
            Assert.Contains("FOREIGN KEY (ReporterUserId) REFERENCES Users(Id)", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithPasswordResetsTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(13);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Id INT PRIMARY KEY AUTO_INCREMENT", result);
            Assert.Contains("UserId INT NOT NULL", result);
            Assert.Contains("Token VARCHAR(255) NOT NULL", result);
            Assert.Contains("ExpirationDate DATETIME NOT NULL", result);
            Assert.Contains("IsUsed BOOLEAN DEFAULT FALSE", result);
            Assert.Contains("FOREIGN KEY (UserId) REFERENCES Users(Id)", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithUserAIResponsesTableQuery_ShouldContainAllRequiredFields()
        {
            // Act
            var result = SqlQueries.SqlQuery(14);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Id INT PRIMARY KEY AUTO_INCREMENT", result);
            Assert.Contains("ResponseText TEXT", result);
            Assert.Contains("CreatedAt DATETIME", result);
            Assert.Contains("UserId INT", result);
            Assert.Contains("FOREIGN KEY (UserId) REFERENCES Users(Id)", result);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithJobSearchQueries_ShouldContainSearchLogic()
        {
            // Act
            var titleSearchQuery = SqlQueries.SqlQuery(103);
            var locationSearchQuery = SqlQueries.SqlQuery(104);
            var combinedSearchQuery = SqlQueries.SqlQuery(106);

            // Assert
            Assert.NotNull(titleSearchQuery);
            Assert.Contains("LOWER(title) LIKE @param1 OR LOWER(description) LIKE @param1", titleSearchQuery);

            Assert.NotNull(locationSearchQuery);
            Assert.Contains("LOWER(location) LIKE @param1", locationSearchQuery);

            Assert.NotNull(combinedSearchQuery);
            Assert.Contains("LOWER(Title) LIKE @param1 OR LOWER(Description) LIKE @param1", combinedSearchQuery);
            Assert.Contains("LOWER(Location) LIKE @param2", combinedSearchQuery);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithDataInsertionQueries_ShouldContainParameterizedQueries()
        {
            // Act
            var insertRawTableQuery = SqlQueries.SqlQuery(100);

            // Assert
            Assert.NotNull(insertRawTableQuery);
            Assert.Contains("@job_id", insertRawTableQuery);
            Assert.Contains("@title", insertRawTableQuery);
            Assert.Contains("@description", insertRawTableQuery);
            Assert.Contains("@company", insertRawTableQuery);
            Assert.Contains("@location", insertRawTableQuery);
            Assert.Contains("@posted_date", insertRawTableQuery);
            Assert.Contains("@webpage_url", insertRawTableQuery);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithDataTransformationQueries_ShouldContainSelectLogic()
        {
            // Act
            var transformQuery = SqlQueries.SqlQuery(101);

            // Assert
            Assert.NotNull(transformQuery);
            Assert.Contains("INSERT INTO Jobs", transformQuery);
            Assert.Contains("SELECT JobId, title, description, company, location, posted_date, webpage_url FROM raw_table", transformQuery);
        }

        [Fact]
        public void SqlQueries_SqlQuery_WithCleanupQueries_ShouldContainDeleteLogic()
        {
            // Act
            var deleteEmailTokensQuery = SqlQueries.SqlQuery(200);
            var deleteDuplicateJobsQuery = SqlQueries.SqlQuery(201);
            var deleteAllJobsQuery = SqlQueries.SqlQuery(203);

            // Assert
            Assert.NotNull(deleteEmailTokensQuery);
            Assert.Contains("DELETE FROM EmailVerificationTokens", deleteEmailTokensQuery);

            Assert.NotNull(deleteDuplicateJobsQuery);
            Assert.Contains("DELETE FROM Jobs WHERE JobId NOT IN (SELECT MIN(JobId) FROM Jobs GROUP BY description)", deleteDuplicateJobsQuery);

            Assert.NotNull(deleteAllJobsQuery);
            Assert.Contains("DELETE FROM Jobs", deleteAllJobsQuery);
        }
    }
}