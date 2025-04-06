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
        int index = 0;
        var transformed = new List<Dictionary<string, object>>();
        foreach (var job in rawJobs)
        {
            
            try
            {
                //Console.WriteLine("Starting transformation for a job...");
                var jobId = job.GetValueOrDefault("id") ?? "Unknown ID";
                //Console.WriteLine($"Job ID: {jobId}");
                var title = job.GetValueOrDefault("headline", "Unknown Title");
                //Console.WriteLine($" Title: {title}");
                
                var rawDescription = job.GetValueOrDefault("description") as Dictionary<string, object>;
                Console.WriteLine($"Type of Raw description: {rawDescription.GetType()}");
                Console.WriteLine($"Raw description: {rawDescription["text"]}");
                var description = rawDescription?.GetValueOrDefault("text", "No description available") ?? "No description available";
                //Console.WriteLine($" Description: {description}");
                //Console.WriteLine($"Job ID: {jobId}, Title: {title}, Description: {description}");
                index++;
                var company = ((Dictionary<string, object>)job.GetValueOrDefault("employer", new Dictionary<string, object>())).GetValueOrDefault("name", "Unknown Company");
                //Console.WriteLine($"Job ID: {jobId}, Title: {title}, Description: {description}, Company: {company}");
                var location = ((Dictionary<string, object>)job.GetValueOrDefault("workplace_address", new Dictionary<string, object>())).GetValueOrDefault("municipality", "Unknown Location");
                //Console.WriteLine($"Job ID: {jobId}, Title: {title}, Description: {description}, Company: {company}, Location: {location}");
                var postedDate = job.GetValueOrDefault("publication_date") ?? DateTime.MinValue;
                //Console.WriteLine($"Job ID: {jobId}, Title: {title}, Description: {description}, Company: {company}, Location: {location}, Posted Date: {postedDate}");
                var webpageUrl = job.GetValueOrDefault("webpage_url") ?? "Unknown URL";

                //Console.WriteLine($"Job ID: {jobId}, Title: {title}, Description: {description}, Company: {company}, Location: {location}, Posted Date: {postedDate}, Webpage URL: {webpageUrl}");

                transformed.Add(new Dictionary<string, object>
                {
                    { "job_id", jobId },
                    { "title", title },
                    { "description", description },
                    { "company", company },
                    { "location", location },
                    { "posted_date", postedDate },
                    { "webpage_url", webpageUrl }
                });
            }
            catch (Exception e)
            {
                //LoggingConfig.ReturnInfo($"Skipping invalid job entry: {e.Message}");
            }
        }
        Console.WriteLine($"Transformed {index} jobs successfully.");
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
            Console.WriteLine($"Number of jobs to insert: {rawJobs.Count}");
            foreach (var job in rawJobs)
            {
                Console.WriteLine($"Job ID: {job.GetValueOrDefault("job_id", "N/A")}, Title: {job.GetValueOrDefault("title", "N/A")}");
            }

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
