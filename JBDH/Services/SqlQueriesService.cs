using System.Collections.Generic;

namespace JBDH.Services
{ 
    public static class SqlQueries
        {
            // BUG :  Number 6 doenst work.
            private static readonly Dictionary<int, string> Queries = new Dictionary<int, string>
            {
                { 1, "CREATE TABLE IF NOT EXISTS raw_table (job_id VARCHAR(255) PRIMARY KEY,title VARCHAR(255),description TEXT,company VARCHAR(255),location VARCHAR(255),posted_date DATE,webpage_url VARCHAR(2083));" }, 
                { 2, "CREATE TABLE IF NOT EXISTS clean_table (job_id VARCHAR(255) PRIMARY KEY, title VARCHAR(255),description TEXT,company VARCHAR(255),location VARCHAR(255),posted_date DATE,webpage_url VARCHAR(2083));" }, 
                { 3, "INSERT INTO raw_table (job_id, title, description, company, location, posted_date, webpage_url) VALUES (@job_id, @title, @description, @company, @location, @posted_date, @webpage_url);" },
                { 5, "INSERT INTO clean_table SELECT DISTINCT * FROM raw_table;" },
                { 6, "DELETE FROM clean_table WHERE job_id NOT IN (SELECT MIN(job_id) FROM clean_table GROUP BY description);" },
                { 8, "SELECT * FROM clean_table WHERE posted_date LIKE @param1 ORDER BY posted_date DESC;" },
                { 9, "SELECT * FROM clean_table WHERE LOWER(title) LIKE @param1 OR LOWER(description) LIKE @param1;" },
                { 10, "SELECT * FROM clean_table WHERE LOWER(location) LIKE @param1;" },
                { 11, "DELETE FROM raw_table;" },
                { 12, "DELETE FROM clean_table;" },
                { 13, "SELECT * FROM clean_table;"},
                { 14, "CREATE TABLE IF NOT EXISTS Users ( Id INT PRIMARY KEY AUTO_INCREMENT, Username VARCHAR(255) NOT NULL UNIQUE, PasswordHash VARCHAR(255) NOT NULL, Role VARCHAR(50), Email VARCHAR(255) NOT NULL UNIQUE, CVPath VARCHAR(2083), Organisation VARCHAR(255), OrganisationID INT NULL, DeveloperMode BOOLEAN DEFAULT FALSE );"},
                { 15, "DELETE FROM Users;" }
            };

            public static string SqlQuery(int queryId)
            {
                return Queries.ContainsKey(queryId) ? Queries[queryId] : null;
            }
        }
    }