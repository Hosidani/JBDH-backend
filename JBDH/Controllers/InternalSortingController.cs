using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using JBDH.Models; // Assuming this is the namespace for your models

namespace JBDH.Controllers; // Assuming this is the namespace for your controller

class InternalSorting
{
    private static readonly string ConnectionString = DbConfig.DB_CONFIG;

    public static void CloseConnection(MySqlConnection conn, MySqlCommand cmd)
    {
        LoggingConfig.ReturnInfo("Closing connection...");
        cmd?.Dispose();
        conn?.Close();
        LoggingConfig.ReturnInfo("Connection closed.");
    }
 
    public static List<Dictionary<string, object>> SortByPostedDate(string date)
    {
        LoggingConfig.ReturnInfo("Sorting by posted date...");
        string query = SqlQueries.SqlQuery(8);
        string searchPattern = date == "default" ? "%" : $"%{date}%";
        return ExecuteQuery(query, new object[] { searchPattern });
    }
 
    public static List<Dictionary<string, object>> SearchByDescription(string description)
    {
        string query = SqlQueries.SqlQuery(9);
        string searchPattern = description == "default" ? "%" : $"%{description.ToLower()}%";
        return ExecuteQuery(query, new object[] { searchPattern });
    }
 
    public static List<Dictionary<string, object>> SearchByLocation(string location)
    {
        string query = SqlQueries.SqlQuery(10);
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
            LoggingConfig.ReturnInfo($"Executing query: {query}");
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
            LoggingConfig.ReturnInfo($"Fetched {results.Count} rows");
        }
        catch (Exception e)
        {
            LoggingConfig.ReturnError($"Query failed: {e.Message}");
            throw;
        }
        return results;
    }
 
    public static List<Dictionary<string, object>> HierarchyResults(params List<Dictionary<string, object>>[] resultsSets)
    {
        LoggingConfig.ReturnInfo("Combining results...");
        var seen = new HashSet<object>();
        var combined = new List<Dictionary<string, object>>();
 
        foreach (var results in resultsSets)
        {
            foreach (var row in results)
            {
                if (!seen.Contains(row["job_id"]))
                {
                    seen.Add(row["job_id"]);
                    combined.Add(row);
                }
            }
        }
        LoggingConfig.ReturnInfo($"Combined {combined.Count} unique results");
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
 
    public static List<Dictionary<string, object>> InternalSortingFunction(string description, string location)
    {
        try
        {
            var results = HierarchyResults(
                SearchByDescription(description),
                SearchByLocation(location)
            );
            LoggingConfig.ReturnInfo($"Returning {results.Count} results");
            return results;
        }
        catch (Exception e)
        {
            LoggingConfig.ReturnError($"Sorting failed: {e.Message}");
            throw;
        }
    }

    public static List<Dictionary<string, object>> GetJobs(string description, string location)
    {
        var jobs = new List<Dictionary<string, object>>();

        try
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            var query = "SELECT * FROM clean_table WHERE (@description = 'default' OR title LIKE @description) AND (@location = 'default' OR location LIKE @location)";
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

}