using System.Collections.Generic;
using MySqlX.XDevAPI.CRUD;

namespace JBDH.Services
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
            { 4, "CREATE TABLE IF NOT EXISTS Users ( Id INT PRIMARY KEY AUTO_INCREMENT, Username VARCHAR(255) NOT NULL UNIQUE, PasswordHash VARCHAR(255) NOT NULL, Email VARCHAR(255) NOT NULL UNIQUE, Location VARCHAR(255), CVPath VARCHAR(2083), Organisation VARCHAR(255), DeveloperMode BOOLEAN DEFAULT FALSE, OpenForHiring BOOLEAN DEFAULT FALSE, Role VARCHAR(100) NOT NULL, IsBanned BOOLEAN DEFAULT FALSE, OwnerId INT NULL, FOREIGN KEY (OwnerId) REFERENCES Users(Id));" },
            { 5, "CREATE TABLE IF NOT EXISTS Friendships (Id INT PRIMARY KEY AUTO_INCREMENT, RequesterId INT NOT NULL,AddresseeId INT NOT NULL, Status ENUM('Pending', 'Friends') NOT NULL, RequestedAt DATETIME DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY (RequesterId) REFERENCES Users(Id), FOREIGN KEY (AddresseeId) REFERENCES Users(Id))"},
            { 6, "CREATE TABLE IF NOT EXISTS EmailVerificationTokens ( Id INT PRIMARY KEY AUTO_INCREMENT, Token VARCHAR(255) NOT NULL, UserId INT NOT NULL, ExpirationDate DATETIME NOT NULL, IsUsed BOOLEAN DEFAULT FALSE, FOREIGN KEY (UserId) REFERENCES Users(Id) );" },
            { 7, "CREATE TABLE IF NOT EXISTS Organisations (Id INT PRIMARY KEY AUTO_INCREMENT, Name VARCHAR(255) NOT NULL UNIQUE, CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP);" },
            { 8, "CREATE TABLE IF NOT EXISTS JobApplications (ApplicationId INT PRIMARY KEY AUTO_INCREMENT, UserId INT NOT NULL, JobId VARCHAR(255) NOT NULL, AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, Notes TEXT);"},
            { 9, "CREATE TABLE IF NOT EXISTS DataFlows (Id INT PRIMARY KEY AUTO_INCREMENT, ContentType VARCHAR(50), Content TEXT, CreatedAt DATETIME, UserId INT, ParentDataFlowId INT NULL, FOREIGN KEY (ParentDataFlowId) REFERENCES DataFlows(Id));" },
            { 10, "CREATE TABLE IF NOT EXISTS JobAIResponses (Id INT AUTO_INCREMENT PRIMARY KEY, JobId VARCHAR(255) NOT NULL, ResponseText TEXT NOT NULL, CreatedAt DATETIME NOT NULL, FOREIGN KEY (JobId) REFERENCES Jobs(JobId));" },
            { 11, "CREATE TABLE IF NOT EXISTS UserRoleAssignments (UserId INT NOT NULL,Role VARCHAR(50) NOT NULL,PRIMARY KEY (UserId, Role),FOREIGN KEY (UserId) REFERENCES Users(Id))" },
            { 12, "CREATE TABLE IF NOT EXISTS UserReports (Id INT PRIMARY KEY AUTO_INCREMENT, ReporterUserId INT NOT NULL, ReportedUserId INT NOT NULL, DataFlowId INT NULL, DataFlowText TEXT NULL, Reason TEXT NOT NULL, CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY (ReportedUserId) REFERENCES Users(Id), FOREIGN KEY (ReporterUserId) REFERENCES Users(Id));" },
            { 13, "CREATE TABLE IF NOT EXISTS PasswordResets (Id INT PRIMARY KEY AUTO_INCREMENT, UserId INT NOT NULL, Token VARCHAR(255) NOT NULL, ExpirationDate DATETIME NOT NULL, IsUsed BOOLEAN DEFAULT FALSE, FOREIGN KEY (UserId) REFERENCES Users(Id));" },

            // 100-199: INSERTS/SELECTS/UPDATES
            { 100, "INSERT INTO raw_table (JobId, title, description, company, location, posted_date, webpage_url) VALUES (@job_id, @title, @description, @company, @location, @posted_date, @webpage_url);" },
            { 101, "INSERT INTO Jobs (JobId, Title, Description, Company, Location, PostedDate, WebpageUrl) SELECT JobId, title, description, company, location, posted_date, webpage_url FROM raw_table;" },
            { 102, "SELECT * FROM Jobs WHERE posted_date LIKE @param1 ORDER BY posted_date DESC;" },
            { 103, "SELECT * FROM Jobs WHERE LOWER(title) LIKE @param1 OR LOWER(description) LIKE @param1;" },
            { 104, "SELECT * FROM Jobs WHERE LOWER(location) LIKE @param1;" },
            { 105, "SELECT * FROM Jobs;" },
            { 106, "SELECT * FROM Jobs WHERE (LOWER(Title) LIKE @param1 OR LOWER(Description) LIKE @param1) AND LOWER(Location) LIKE @param2;" },

            // 200-299: DELETES
            { 200, "DELETE FROM EmailVerificationTokens;" },
            { 201, "DELETE FROM Jobs WHERE JobId NOT IN (SELECT MIN(JobId) FROM Jobs GROUP BY description);" },
            { 202, "DELETE FROM raw_table;" },
            { 203, "DELETE FROM Jobs;" },
            { 204, "DELETE FROM Users;" },
            { 205, "DELETE FROM AIResponses;" },
            { 206, "DELETE FROM UserRoleAssignments;" }
        };

        public static string? SqlQuery(int queryId)
        {
            return Queries.ContainsKey(queryId) ? Queries[queryId] : null;
        }
    }
}