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
        LoggingConfig.ReturnInfo("Starting data extraction...");
        LoggingConfig.ReturnInfo($"Extracting data from {filePath}...");
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
        LoggingConfig.ReturnInfo("Starting data transformation...");
        LoggingConfig.ReturnInfo($"Number of jobs to transform: {rawJobs.Count}");
        var transformed = new List<Dictionary<string, object>>();
        int warningCount = 0;

        int missingDescriptionCount = 0;
        int missingTitleCount = 0;
        int missingEmployerCount = 0;
        int missingLocationCount = 0;

        foreach (var job in rawJobs)
        {
            if (job.GetValueOrDefault("description") == null) missingDescriptionCount++;
            if (job.GetValueOrDefault("headline") == null) missingTitleCount++;
            if (job.GetValueOrDefault("employer") == null) missingEmployerCount++;
            if (job.GetValueOrDefault("workplace_address") == null) missingLocationCount++;
        }
        LoggingConfig.ReturnInfo($"Number of jobs with missing fields: {missingDescriptionCount + missingTitleCount + missingEmployerCount + missingLocationCount}");

        foreach (var job in rawJobs)
        {
            try
            {
                var jobId = job.GetValueOrDefault("id") ?? "Unknown ID";
                var rawTitle = job.GetValueOrDefault("headline", "Unknown Title").ToString();
                var title = rawTitle != null ? DecodeUnicodeString(rawTitle) : "Unknown Title";

                // Define the DecodeUnicodeString method
                static string DecodeUnicodeString(string input)
                {
                    return System.Text.RegularExpressions.Regex.Unescape(input);
                }

                // Handle the "description" field
                var rawDescription = job.GetValueOrDefault("description");
                string description = "No Description";
                if (rawDescription is JsonElement descriptionElement && descriptionElement.ValueKind == JsonValueKind.Object)
                {
                    if (descriptionElement.TryGetProperty("text", out JsonElement textElement))
                    {
                        try
                        {
                            description = textElement.GetString() ?? "No Description";
                            description = DecodeUnicodeString(description);

                            // Replace line breaks and sanitize the text
                            description = description.Replace("\n", " ").Replace("\r", " ").Trim();

                            // Truncate the description to 200 characters
                            int maxLength = 200;
                            if (description.Length > maxLength)
                            {
                                description = description.Substring(0, maxLength) + "...";
                            }
                        }
                        catch (Exception ex)
                        {
                            var truncatedMessage = ex.Message.Length > 100 ? ex.Message.Substring(0, 50) + "..." : ex.Message;
                            LoggingConfig.ReturnError($"Processing description: {truncatedMessage}");
                            description = "Invalid Description Format";
                        }
                    }
                    else
                    {
                        LoggingConfig.ReturnError("The 'text' field is missing in the 'description' object.");
                    }
                }
                else if (rawDescription == null)
                {
                    LoggingConfig.ReturnError("Warning: The 'description' field is null or missing.");
                    warningCount++;
                }
                else
                {
                    LoggingConfig.ReturnError("Warning: The 'description' field is not a valid JSON object.");
                    warningCount++;
                }
 
                string company = "Unknown Company";
                var rawCompany = job.GetValueOrDefault("employer");
                if (rawCompany is JsonElement employerElement && employerElement.ValueKind == JsonValueKind.Object)
                {
                    if (employerElement.TryGetProperty("workplace", out JsonElement workplaceElement))
                    {
                        try
                        {
                            company = workplaceElement.GetString() ?? "No company name";
                            company = DecodeUnicodeString(company);

                            // Replace line breaks and sanitize the text
                            company = company.Replace("\n", " ").Replace("\r", " ").Trim();
                        }
                        catch (Exception ex)
                        {
                            // Limit the exception message to 100 characters to avoid overly verbose output
                            var truncatedMessage = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) + "..." : ex.Message;
                            LoggingConfig.ReturnError($"Error processing company: {truncatedMessage}");
                            company = "Invalid company name";
                        }
                    }
                }

                // Handle the "workplace_address" field
                string location = "Unknown Location";
                if (job.GetValueOrDefault("workplace_address") is JsonElement addressElement && addressElement.ValueKind == JsonValueKind.Object)
                {
                    if (addressElement.TryGetProperty("municipality", out JsonElement municipalityElement))
                    {
                        location = DecodeUnicodeString(municipalityElement.GetString() ?? "Unknown Location");
                    }
                }

                // Handle the "publication_date" field
                var postedDate = job.GetValueOrDefault("publication_date") ?? DateTime.MinValue;

                // Handle the "webpage_url" field
                var webpageUrl = job.GetValueOrDefault("webpage_url") ?? "Unknown URL";

                // Add the transformed job to the list
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
                LoggingConfig.ReturnInfo($"Skipping invalid job entry: {e.Message}");
            }
        }
        LoggingConfig.ReturnInfo($"Transformed {transformed.Count} jobs successfully.");
        LoggingConfig.ReturnInfo($"Number of warnings: {warningCount}");
        return transformed;
    }

/*
    public static List<Dictionary<string, object>> TransformJobs(List<Dictionary<string, object>> rawJobs)
    {
        var transformedJobs = new List<Dictionary<string, object>>();

        foreach (var job in rawJobs)
        {
            // Extract the "title" field
            var title = job.GetValueOrDefault("headline", "N/A");

            // Extract the "location" field
            string location = "N/A";
            if (job.GetValueOrDefault("workplace_address") is JsonElement addressElement && addressElement.ValueKind == JsonValueKind.Object)
            {
                if (addressElement.TryGetProperty("municipality", out JsonElement municipalityElement))
                {
                    location = municipalityElement.GetString() ?? "N/A";
                }
            }

            // Extract the "description" field
            string description = "N/A";
            if (job.GetValueOrDefault("description") is JsonElement descriptionElement && descriptionElement.ValueKind == JsonValueKind.Object)
            {
                if (descriptionElement.TryGetProperty("text", out JsonElement textElement))
                {
                    description = textElement.GetString() ?? "N/A";
                }
            }

            // Extract the "employer" field
            string company = "N/A";
            if (job.GetValueOrDefault("employer") is JsonElement employerElement && employerElement.ValueKind == JsonValueKind.Object)
            {
                if (employerElement.TryGetProperty("name", out JsonElement nameElement))
                {
                    company = nameElement.GetString() ?? "N/A";
                }
            }

            // Add the transformed job to the list
            var transformedJob = new Dictionary<string, object>
            {
                { "title", title },
                { "location", location },
                { "description", description },
                { "company", company }
            };

            transformedJobs.Add(transformedJob);
        }

        return transformedJobs;
    }
*/
    public static void LoadData(List<Dictionary<string, object>> rawJobs)
    {
        try
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            LoggingConfig.ReturnInfo("Database connection established.");
            LoggingConfig.ReturnInfo("Starting Load data process...");
            LoggingConfig.ReturnInfo($"Number of jobs to insert: {rawJobs.Count}");
            /*foreach (var job in rawJobs)
            {
                Console.WriteLine($"Job ID: {job.GetValueOrDefault("job_id", "N/A")}, Title: {job.GetValueOrDefault("title", "N/A")}");
            }*/

            // Step 1: Insert raw jobs into raw_table
            foreach (var job in rawJobs)
            {
                //LoggingConfig.ReturnInfo($"Inserting job_id {job["job_id"]} into raw_table...");
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
                    LoggingConfig.ReturnError($"MySQL error inserting job_id {job["job_id"]}: {ex.Message}");
                }
            }
            LoggingConfig.ReturnInfo("All jobs inserted into raw_table.");

            // Step 2: Deduplicate data by inserting distinct rows into clean_table
            var deduplicateCommand = new MySqlCommand(SqlQueries.SqlQuery(5), connection);
            deduplicateCommand.ExecuteNonQuery();
            LoggingConfig.ReturnInfo("Distinct rows inserted into clean_table.");
            LoggingConfig.ReturnInfo("ETL process completed successfully.");
        }
        catch (Exception e)
        {
            LoggingConfig.ReturnError($"Error in ETL process: {e.Message}");
        }
    }

    public static void RunEtlPipeline(string jsonFile)
    {
        LoggingConfig.ReturnInfo("Starting ETL pipeline...");
        LoggingConfig.ReturnInfo("Extracting raw data from JSON file...");
        var rawData = ExtractData(jsonFile);
        LoggingConfig.ReturnInfo("Data extracted...");
        LoggingConfig.ReturnInfo("Transforming data...");
        var sqlData = TransformData(rawData);
        LoggingConfig.ReturnInfo("Data transformed...");
        LoggingConfig.ReturnInfo("Processing jobs...");
        // Call LoadData to handle loading and deduplication
        LoadData(sqlData);
        LoggingConfig.ReturnInfo("ETL pipeline completed successfully.");
    }
}
