using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using JBDH.Models; // Assuming this is the namespace for your models

namespace JBDH.Services
{ 
    class InternalSortingService
    {
        private static readonly string ConnectionString = DbConfig.DB_CONFIG;

        public static void CloseConnection(MySqlConnection conn, MySqlCommand cmd)
        {
            LoggingConfigService.ReturnInfo("Closing connection...");
            cmd?.Dispose();
            conn?.Close();
            LoggingConfigService.ReturnInfo("Connection closed.");
        }
    
        public static List<Dictionary<string, object>> SortByPostedDate(string date)
        {
            LoggingConfigService.ReturnInfo("Sorting by posted date...");
            string query = SqlQueries.SqlQuery(102) ?? "";
            string searchPattern = date == "default" ? "%" : $"%{date}%";
            return ExecuteQuery(query, new object[] { searchPattern });
        }
        
        
        public static List<Dictionary<string, object>> SearchByDescriptionAndLocation(string description, string location)
        {
            string query = SqlQueries.SqlQuery(106) ?? "";
            string descPattern = string.IsNullOrEmpty(description) ? "%" : $"%{description.ToLower()}%";
            string locPattern = string.IsNullOrEmpty(location) ? "%" : $"%{location.ToLower()}%";
            return ExecuteQuery(query, new object[] { descPattern, locPattern });
        }
        
        public static List<Dictionary<string, object>> SearchByDescription(string description)
        {
            string query = SqlQueries.SqlQuery(103) ?? "";
            string searchPattern = description == "default" ? "%" : $"%{description.ToLower()}%";
            return ExecuteQuery(query, new object[] { searchPattern });
        }
    
        public static List<Dictionary<string, object>> SearchByLocation(string location)
        {
            string query = SqlQueries.SqlQuery(104) ?? "";
            string searchPattern = location == "default" ? "%" : $"%{location.ToLower()}%";
            return ExecuteQuery(query, new object[] { searchPattern });
        }

        public static List<Dictionary<string, object>> ExecuteQuery(string query, object[] parameters)
        {
            using var conn = new MySqlConnection(Settings.SetupConnection());
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@param{i + 1}", parameters[i]);
                }
            }
            var results = new List<Dictionary<string, object>>();
            try
            {
                LoggingConfigService.ReturnInfo($"Executing query: {query}");
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }
                LoggingConfigService.ReturnInfo($"Fetched {results.Count} rows");
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Query failed: {e.Message}");
                throw;
            }
            return results;
        }
    
        public static List<Dictionary<string, object>> HierarchyResults(params List<Dictionary<string, object>>[] resultsSets)
        {
            LoggingConfigService.ReturnInfo("Combining results...");
            var seen = new HashSet<object>();
            var combined = new List<Dictionary<string, object>>();
            foreach (var results in resultsSets)
            {
                foreach (var row in results)
                {
                    if (!seen.Contains(row["JobId"]))
                    {
                        seen.Add(row["JobId"]);
                        combined.Add(row);
                    }
                }
            }
            LoggingConfigService.ReturnInfo($"Combined {combined.Count} unique results");
            return combined;
        }
    
        public static (string, string, string) GetInputFromUser(string description, string location, string date)
        {
            return (
                string.IsNullOrEmpty(description) ? "default" : description,
                string.IsNullOrEmpty(location) ? "default" : location,
                string.IsNullOrEmpty(date) ? "default" : date
            );
        }

        public static List<Dictionary<string, object>> GetJobs(string description, string location)
        {
            var jobs = new List<Dictionary<string, object>>();
            try
            {
                using var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                var query = "SELECT * FROM Jobs WHERE (@description = 'default' OR title LIKE @description) AND (@location = 'default' OR location LIKE @location)";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@description", string.IsNullOrEmpty(description) ? "default" : $"%{description}%");
                command.Parameters.AddWithValue("@location", string.IsNullOrEmpty(location) ? "default" : $"%{location}%");
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var job = new Dictionary<string, object>
                    {
                        { "title", reader["title"] },
                        { "company", reader["company"] },
                        { "location", reader["location"] },
                        { "description", reader["description"] }
                    };
                    jobs.Add(job);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error retrieving jobs: {e.Message}");
            }
            return jobs;
        }
        public static List<Dictionary<string, object>> InternalSortingFunction(string description, string location)
        {
            if (!string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(location))
            {
                // Search by both
                return SearchByDescriptionAndLocation(description, location);
            }
            else if (!string.IsNullOrEmpty(description))
            {
                return SearchByDescription(description);
            }
            else if (!string.IsNullOrEmpty(location))
            {
                return SearchByLocation(location);
            }
            else
            {
                return new List<Dictionary<string, object>>();
            }
        }

    }
}