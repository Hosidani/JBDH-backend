using System.Collections.Generic;
using MySqlX.XDevAPI.CRUD;

namespace JBDH.Services.Core
{ 
    public static class SqlQueries
    {
        // BUG : Number 6 doenst work.
        // BUG : Change name of the tables to PascalCase.
        private static readonly Dictionary<int, string> Queries = new Dictionary<int, string>
        {
            // 0-99: CREATE TABLE/DB
            { 0, "CREATE DATABASE IF NOT EXISTS testing_purposes"},
            { 1, "CREATE TABLE IF NOT EXISTS raw_table (JobId VARCHAR(255) PRIMARY KEY,title VARCHAR(255),description TEXT,company VARCHAR(255),location VARCHAR(255),posted_date DATE,webpage_url VARCHAR(2083));" },
            { 2, "CREATE TABLE IF NOT EXISTS Jobs (JobId VARCHAR(255) PRIMARY KEY, Title VARCHAR(255), Description TEXT, Company VARCHAR(255), Location VARCHAR(255), PostedDate DATE, WebpageUrl VARCHAR(2083), RecruiterId INT NULL, FOREIGN KEY (RecruiterId) REFERENCES Users(Id));" },
            { 3, "CREATE TABLE IF NOT EXISTS AIResponses (Id INT PRIMARY KEY AUTO_INCREMENT,Discriminator VARCHAR(128) NOT NULL DEFAULT 'AIResponse',ResponseText TEXT,CreatedAt DATETIME,UserId INT,FOREIGN KEY (UserId) REFERENCES Users(Id));" },
            { 4, "CREATE TABLE IF NOT EXISTS Users ( Id INT PRIMARY KEY AUTO_INCREMENT, Username VARCHAR(255) NOT NULL UNIQUE, PasswordHash VARCHAR(255) NOT NULL, Email VARCHAR(255) NOT NULL UNIQUE, Location VARCHAR(255), CVPath VARCHAR(2083), Organisation VARCHAR(255), DeveloperMode BOOLEAN DEFAULT FALSE, OpenForHiring BOOLEAN DEFAULT FALSE, Role VARCHAR(100) NOT NULL, IsBanned BOOLEAN DEFAULT FALSE, OwnerId INT NULL, AboutMe TEXT NULL, FOREIGN KEY (OwnerId) REFERENCES Users(Id));" },
            { 5, "CREATE TABLE IF NOT EXISTS Friendships (Id INT PRIMARY KEY AUTO_INCREMENT, RequesterId INT NOT NULL, AddresseeId INT NOT NULL, Status INT NOT NULL, RequestedAt DATETIME DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY (RequesterId) REFERENCES Users(Id), FOREIGN KEY (AddresseeId) REFERENCES Users(Id))"},
            { 6, "CREATE TABLE IF NOT EXISTS EmailVerificationTokens ( Id INT PRIMARY KEY AUTO_INCREMENT, Token VARCHAR(255) NOT NULL, UserId INT NOT NULL, ExpirationDate DATETIME NOT NULL, IsUsed BOOLEAN DEFAULT FALSE, FOREIGN KEY (UserId) REFERENCES Users(Id) );" },
            { 7, "CREATE TABLE IF NOT EXISTS Organisations (Id INT PRIMARY KEY AUTO_INCREMENT, Name VARCHAR(255) NOT NULL UNIQUE, CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP);" },
            { 8, "CREATE TABLE IF NOT EXISTS JobApplications (ApplicationId INT PRIMARY KEY AUTO_INCREMENT, UserId INT NOT NULL, JobId VARCHAR(255) NOT NULL, AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, Notes TEXT);"},
            { 9, "CREATE TABLE IF NOT EXISTS DataFlows (Id INT PRIMARY KEY AUTO_INCREMENT, ContentType VARCHAR(50), Content TEXT, CreatedAt DATETIME, UserId INT, ParentDataFlowId INT NULL, FOREIGN KEY (ParentDataFlowId) REFERENCES DataFlows(Id));" },
            { 10, "CREATE TABLE IF NOT EXISTS JobAIResponses (Id INT AUTO_INCREMENT PRIMARY KEY, JobId VARCHAR(255) NOT NULL, ResponseText TEXT NOT NULL, CreatedAt DATETIME NOT NULL, FOREIGN KEY (JobId) REFERENCES Jobs(JobId));" },
            { 11, "CREATE TABLE IF NOT EXISTS UserRoleAssignments (UserId INT NOT NULL,Role VARCHAR(50) NOT NULL,PRIMARY KEY (UserId, Role),FOREIGN KEY (UserId) REFERENCES Users(Id))" },
            { 12, "CREATE TABLE IF NOT EXISTS UserReports (Id INT PRIMARY KEY AUTO_INCREMENT, ReporterUserId INT NOT NULL, ReportedUserId INT NOT NULL, DataFlowId INT NULL, DataFlowText TEXT NULL, Reason TEXT NOT NULL, CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY (ReportedUserId) REFERENCES Users(Id), FOREIGN KEY (ReporterUserId) REFERENCES Users(Id));" },
            { 13, "CREATE TABLE IF NOT EXISTS PasswordResets (Id INT PRIMARY KEY AUTO_INCREMENT, UserId INT NOT NULL, Token VARCHAR(255) NOT NULL, ExpirationDate DATETIME NOT NULL, IsUsed BOOLEAN DEFAULT FALSE, FOREIGN KEY (UserId) REFERENCES Users(Id));" },
            { 14, "CREATE TABLE IF NOT EXISTS UserAIResponses (Id INT PRIMARY KEY AUTO_INCREMENT, ResponseText TEXT, CreatedAt DATETIME, UserId INT, FOREIGN KEY (UserId) REFERENCES Users(Id));" },
            { 15, "CREATE TABLE IF NOT EXISTS Boards (Id INT PRIMARY KEY AUTO_INCREMENT, Name VARCHAR(255) NOT NULL, OwnerId INT NOT NULL, IsOrganizationBoard BOOLEAN DEFAULT FALSE, OrganizationId INT NULL, CreatedAt DATETIME NOT NULL, UpdatedAt DATETIME NOT NULL, FOREIGN KEY (OwnerId) REFERENCES Users(Id), FOREIGN KEY (OrganizationId) REFERENCES Organisations(Id));" },
            { 16, "CREATE TABLE IF NOT EXISTS Lists (Id INT PRIMARY KEY AUTO_INCREMENT, BoardId INT NOT NULL, Name VARCHAR(255) NOT NULL, `Order` INT NOT NULL, CreatedAt DATETIME NOT NULL, FOREIGN KEY (BoardId) REFERENCES Boards(Id));" },
            { 17, "CREATE TABLE IF NOT EXISTS Cards (Id INT PRIMARY KEY AUTO_INCREMENT, ListId INT NOT NULL, Title VARCHAR(255) NOT NULL, Description TEXT, `Order` INT NOT NULL, CreatedAt DATETIME NOT NULL, DueDate DATETIME NULL, AssignedUserId INT NULL, FOREIGN KEY (ListId) REFERENCES Lists(Id), FOREIGN KEY (AssignedUserId) REFERENCES Users(Id));" },
            { 18, "CREATE TABLE IF NOT EXISTS BoardMemberships (Id INT PRIMARY KEY AUTO_INCREMENT, BoardId INT NOT NULL, UserId INT NOT NULL, Role VARCHAR(50) NOT NULL, InvitedByUserId INT, InvitationAccepted BOOLEAN DEFAULT FALSE, InvitationToken VARCHAR(255), CreatedAt DATETIME NOT NULL, FOREIGN KEY (BoardId) REFERENCES Boards(Id), FOREIGN KEY (UserId) REFERENCES Users(Id), FOREIGN KEY (InvitedByUserId) REFERENCES Users(Id));" },

            // 100-199: INSERTS/SELECTS/UPDATES
            { 100, "INSERT INTO raw_table (JobId, title, description, company, location, posted_date, webpage_url) VALUES (@job_id, @title, @description, @company, @location, @posted_date, @webpage_url);" },
            { 101, "INSERT INTO Jobs (JobId, Title, Description, Company, Location, PostedDate, WebpageUrl) SELECT JobId, title, description, company, location, posted_date, webpage_url FROM raw_table;" },
            { 102, "SELECT * FROM Jobs WHERE posted_date LIKE @param1 ORDER BY posted_date DESC;" },
            { 103, "SELECT * FROM Jobs WHERE LOWER(title) LIKE @param1 OR LOWER(description) LIKE @param1;" },
            { 104, "SELECT * FROM Jobs WHERE LOWER(location) LIKE @param1;" },
            { 105, "SELECT * FROM Jobs;" },
            { 106, "SELECT * FROM Jobs WHERE (LOWER(Title) LIKE @param1 OR LOWER(Description) LIKE @param1) AND LOWER(Location) LIKE @param2;" },
            { 107, "SELECT * FROM Jobs WHERE LOWER(title) LIKE @param1" },
            { 108, "INSERT INTO Users (Username, PasswordHash, Email, Location, CVPath, Organisation, DeveloperMode, OpenForHiring, Role, IsBanned, OwnerId, AboutMe) VALUES (@Username, @PasswordHash, @Email, @Location, @CVPath, @Organisation, @DeveloperMode, @OpenForHiring, @Role, @IsBanned, @OwnerId, @AboutMe);" },

            // 200-299: DELETES
            { 200, "DELETE FROM EmailVerificationTokens;" },
            { 201, "DELETE FROM Jobs WHERE JobId NOT IN (SELECT MIN(JobId) FROM Jobs GROUP BY description);" },
            { 202, "DELETE FROM raw_table;" },
            { 203, "DELETE FROM Jobs;" },
            { 204, "DELETE FROM Users;" },
            { 205, "DELETE FROM AIResponses;" },
            { 206, "DELETE FROM UserRoleAssignments;" },

            // 300: INSERT INTO Boards
            { 300, "INSERT INTO Boards (Name, OwnerId, IsOrganizationBoard, OrganizationId, CreatedAt, UpdatedAt) VALUES (@Name, @OwnerId, @IsOrganizationBoard, @OrganizationId, @CreatedAt, @UpdatedAt);" },

            // 301-309: INSERTS
            { 301, "INSERT INTO Lists (BoardId, Name, `Order`, CreatedAt) VALUES (@BoardId, @Name, @Order, @CreatedAt);" },
            { 302, "INSERT INTO Cards (ListId, Title, Description, `Order`, CreatedAt, DueDate, AssignedUserId) VALUES (@ListId, @Title, @Description, @Order, @CreatedAt, @DueDate, @AssignedUserId);" },
            { 303, "INSERT INTO BoardMemberships (BoardId, UserId, Role, InvitedByUserId, InvitationAccepted, InvitationToken, CreatedAt) VALUES (@BoardId, @UserId, @Role, @InvitedByUserId, @InvitationAccepted, @InvitationToken, @CreatedAt);" },

            // 310-319: UPDATES
            { 310, "UPDATE Boards SET Name = @Name, UpdatedAt = @UpdatedAt WHERE Id = @Id;" },
            { 311, "UPDATE Lists SET Name = @Name, `Order` = @Order WHERE Id = @Id;" },
            { 312, "UPDATE Cards SET Title = @Title, Description = @Description, `Order` = @Order, DueDate = @DueDate, AssignedUserId = @AssignedUserId WHERE Id = @Id;" },
            { 313, "UPDATE BoardMemberships SET Role = @Role, InvitationAccepted = @InvitationAccepted WHERE Id = @Id;" },
            { 314, "UPDATE Users SET AboutMe = @AboutMe WHERE Id = @UserId;" },

            // 320-329: DELETES
            { 320, "DELETE FROM Boards WHERE Id = @Id;" },
            { 321, "DELETE FROM Lists WHERE Id = @Id;" },
            { 322, "DELETE FROM Cards WHERE Id = @Id;" },
            { 323, "DELETE FROM BoardMemberships WHERE Id = @Id;" }
        };

        public static string? SqlQuery(int queryId)
        {
            return Queries.ContainsKey(queryId) ? Queries[queryId] : null;
        }
    }
}