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
                { 0, "CREATE DATABASE IF NOT EXISTS testing_purposes"},
                { 1, "CREATE TABLE IF NOT EXISTS raw_table (job_id VARCHAR(255) PRIMARY KEY,title VARCHAR(255),description TEXT,company VARCHAR(255),location VARCHAR(255),posted_date DATE,webpage_url VARCHAR(2083));" },
                { 2, "CREATE TABLE IF NOT EXISTS clean_table (job_id VARCHAR(255) PRIMARY KEY, title VARCHAR(255),description TEXT,company VARCHAR(255),location VARCHAR(255),posted_date DATE,webpage_url VARCHAR(2083));" },
                { 3, "INSERT INTO raw_table (job_id, title, description, company, location, posted_date, webpage_url) VALUES (@job_id, @title, @description, @company, @location, @posted_date, @webpage_url);" },
                { 4, "DELETE FROM EmailVerificationTokens;" },
                { 5, "INSERT INTO clean_table SELECT DISTINCT * FROM raw_table;" },
                { 6, "DELETE FROM clean_table WHERE job_id NOT IN (SELECT MIN(job_id) FROM clean_table GROUP BY description);" },
                { 7, "CREATE TABLE IF NOT EXISTS AIResponses (Id INT PRIMARY KEY AUTO_INCREMENT,Discriminator VARCHAR(128) NOT NULL DEFAULT 'AIResponse',ResponseText TEXT,CreatedAt DATETIME,UserId INT,FOREIGN KEY (UserId) REFERENCES Users(Id));" },
                { 8, "SELECT * FROM clean_table WHERE posted_date LIKE @param1 ORDER BY posted_date DESC;" },
                { 9, "SELECT * FROM clean_table WHERE LOWER(title) LIKE @param1 OR LOWER(description) LIKE @param1;" },
                { 10, "SELECT * FROM clean_table WHERE LOWER(location) LIKE @param1;" },
                { 11, "DELETE FROM raw_table;" },
                { 12, "DELETE FROM clean_table;" },
                { 13, "SELECT * FROM clean_table;"},
                { 14, "CREATE TABLE IF NOT EXISTS Users ( Id INT PRIMARY KEY AUTO_INCREMENT, Username VARCHAR(255) NOT NULL UNIQUE, PasswordHash VARCHAR(255) NOT NULL, Role VARCHAR(50), Email VARCHAR(255) NOT NULL UNIQUE, CVPath VARCHAR(2083), Organisation VARCHAR(255), DeveloperMode BOOLEAN DEFAULT FALSE, OpenForHiring BOOLEAN DEFAULT FALSE);" },
                { 15, "DELETE FROM Users;" },
                { 16, "CREATE TABLE IF NOT EXISTS Friendships (Id INT PRIMARY KEY AUTO_INCREMENT, RequesterId INT NOT NULL,AddresseeId INT NOT NULL, Status ENUM('Pending', 'Friends') NOT NULL, RequestedAt DATETIME DEFAULT CURRENT_TIMESTAMP, FOREIGN KEY (RequesterId) REFERENCES Users(Id), FOREIGN KEY (AddresseeId) REFERENCES Users(Id))"},
                { 17, "CREATE TABLE IF NOT EXISTS EmailVerificationTokens ( Id INT PRIMARY KEY AUTO_INCREMENT, Token VARCHAR(255) NOT NULL, UserId INT NOT NULL, ExpirationDate DATETIME NOT NULL, IsUsed BOOLEAN DEFAULT FALSE, FOREIGN KEY (UserId) REFERENCES Users(Id) );" },
                { 18, "CREATE TABLE IF NOT EXISTS Organisations (Id INT PRIMARY KEY AUTO_INCREMENT, Name VARCHAR(255) NOT NULL UNIQUE, CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP);" },
                { 19, "CREATE TABLE IF NOT EXISTS JobApplications (ApplicationId INT PRIMARY KEY AUTO_INCREMENT, UserId INT NOT NULL, JobId VARCHAR(255) NOT NULL, AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, Notes TEXT);"},
                { 20, "CREATE TABLE IF NOT EXISTS DataFlows (Id INT PRIMARY KEY AUTO_INCREMENT, ContentType VARCHAR(50), Content TEXT, CreatedAt DATETIME, UserId INT);" },
                { 21, "DELETE FROM AIResponses;" }
            };

        public static string SqlQuery(int queryId)
        {
            return Queries.ContainsKey(queryId) ? Queries[queryId] : null;
        }
    }
}