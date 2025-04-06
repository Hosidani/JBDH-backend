using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MySql.Data.MySqlClient;
using JBDH.Models; // Assuming this is the namespace for your models

namespace JBDH.Controllers; // Assuming this is the namespace for your controller

public class EtlPipeline
{
    private static readonly string ConnectionString = DbConfig.DB_CONFIG;

    public static DateTime? ParseDate(string dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        if (dateStr.EndsWith("Z")) dateStr = dateStr[..^1];
        return DateTime.ParseExact(dateStr, "yyyy-MM-ddTHH:mm:ss", null);
    }

    public static List<Dictionary<string, object>> ExtractData(string filePath)
    {
        try
        {
            var jsonData = File.ReadAllText(filePath);
            var result = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonData);
            return result ?? new List<Dictionary<string, object>>(); // Handle possible null return
        }
        catch (Exception e)
        {
            LoggingConfig.ReturnInfo($"Error reading JSON file: {e.Message}");
            return new List<Dictionary<string, object>>();
        }
    }

    public static List<Dictionary<string, object>> TransformData(List<Dictionary<string, object>> rawJobs)
    {
        var transformed = new List<Dictionary<string, object>>();
        foreach (var job in rawJobs)
        {
            try
            {
                transformed.Add(new Dictionary<string, object>
                {
                    { "job_id", job.GetValueOrDefault("id") ?? "Unknown ID" }, // Provide a default value
                    { "title", job.GetValueOrDefault("headline", "Unknown Title") },
                    { "description", ((Dictionary<string, object>)job.GetValueOrDefault("description", new Dictionary<string, object>())).GetValueOrDefault("text", "No Description") },
                    { "company", ((Dictionary<string, object>)job.GetValueOrDefault("employer", new Dictionary<string, object>())).GetValueOrDefault("name", "Unknown Company") },
                    { "location", ((Dictionary<string, object>)job.GetValueOrDefault("workplace_address", new Dictionary<string, object>())).GetValueOrDefault("municipality", "Unknown Location") },
                    { "posted_date", job.GetValueOrDefault("publication_date") ?? DateTime.MinValue }, // Default to DateTime.MinValue
                    { "webpage_url", job.GetValueOrDefault("webpage_url") ?? "Unknown URL" } // Provide a default value
                });
            }
            catch (Exception e)
            {
                LoggingConfig.ReturnInfo($"Skipping invalid job entry: {e.Message}");
            }
        }
        return transformed;
    }

    public static List<Dictionary<string, object>> TransformJobs(List<Dictionary<string, object>> rawJobs)
    {
        var transformedJobs = new List<Dictionary<string, object>>();

        foreach (var job in rawJobs)
        {
            var transformedJob = new Dictionary<string, object>
            {
                { "title", job.GetValueOrDefault("headline", "N/A") },
                { "location", ((Dictionary<string, object>)job.GetValueOrDefault("workplace_address", new Dictionary<string, object>()))
                    .GetValueOrDefault("municipality", "N/A") },
                { "description", ((Dictionary<string, object>)job.GetValueOrDefault("description", new Dictionary<string, object>()))
                    .GetValueOrDefault("text", "N/A") }
            };

            transformedJobs.Add(transformedJob);
        }

        return transformedJobs;
    }

public static void LoadData(List<Dictionary<string, object>> rawJobs)
    {
        try
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();

            Console.WriteLine("Starting ETL process...");

            // Step 1: Insert raw jobs into raw_table
            foreach (var job in rawJobs)
            {
                LoggingConfig.ReturnInfo($"Inserting job_id {job["job_id"]} into raw_table...");
                var command = new MySqlCommand(SqlQueries.SqlQuery(3), connection);
                command.Parameters.AddWithValue("@job_id", job["job_id"]);
                command.Parameters.AddWithValue("@title", job["title"]);
                command.Parameters.AddWithValue("@description", job["description"]);
                command.Parameters.AddWithValue("@company", job["company"]);
                command.Parameters.AddWithValue("@location", job["location"]);
                command.Parameters.AddWithValue("@posted_date", job["posted_date"]);
                command.Parameters.AddWithValue("@webpage_url", job["webpage_url"]);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    Console.Error.WriteLine($"Error inserting job_id {job["job_id"]}: {ex.Message}");
                }
            }

            Console.WriteLine("Raw jobs inserted into raw_table.");

            // Step 2: Deduplicate data by inserting distinct rows into clean_table
            var deduplicateCommand = new MySqlCommand(SqlQueries.SqlQuery(5), connection);
            deduplicateCommand.ExecuteNonQuery();
            Console.WriteLine("Distinct rows inserted into clean_table.");

            // Step 3: Remove duplicates from raw_table
            var cleanupCommand = new MySqlCommand(SqlQueries.SqlQuery(6), connection);
            cleanupCommand.ExecuteNonQuery();
            Console.WriteLine("Duplicates removed from raw_table.");

            Console.WriteLine("ETL process completed successfully.");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error in ETL process: {e.Message}");
        }
    }
    public static void RunEtlPipeline(string jsonFile)
    {
        LoggingConfig.ReturnInfo("Starting ETL pipeline...\nExtracting raw data from JSON file...");
        var rawData = ExtractData(jsonFile);
        LoggingConfig.ReturnInfo("Data extracted...\nTransforming data...");
        var sqlData = TransformData(rawData);
        LoggingConfig.ReturnInfo("Data transformed...\nProcessing jobs...");

        // Call ProcessJobs to handle loading and deduplication
        LoadData(sqlData);

        LoggingConfig.ReturnInfo("ETL pipeline completed successfully.");
    }
}
