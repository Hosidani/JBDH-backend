using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MySql.Data.MySqlClient;
using JBDH.Models; // Assuming this is the namespace for your models
using NLog.Fluent; // Assuming this is the namespace for your models

namespace JBDH.Services
{    

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
        LoggingConfigService.ReturnInfo("Extracting raw data from JSON file...");
        LoggingConfigService.ReturnInfo($"Extracting data from {filePath}...");
        try
        {
            var jsonData = File.ReadAllText(filePath);
            var result = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonData);
            return result ?? new List<Dictionary<string, object>>(); // Handle possible null return
        }
        catch (Exception e)
        {
            LoggingConfigService.ReturnInfo($"Error reading JSON file: {e.Message}");
            return new List<Dictionary<string, object>>();
        }
    }

    public static List<Dictionary<string, object>> TransformData(List<Dictionary<string, object>> rawJobs)
    {
        LoggingConfigService.ReturnInfo("Transforming data...");
        LoggingConfigService.ReturnInfo($"Number of jobs to transform: {rawJobs.Count}");
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
        LoggingConfigService.ReturnInfo($"Number of jobs with missing fields: {missingDescriptionCount + missingTitleCount + missingEmployerCount + missingLocationCount}");

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
                            LoggingConfigService.ReturnError($"Processing description: {truncatedMessage}");
                            description = "Invalid Description Format";
                        }
                    }
                    else
                    {
                        LoggingConfigService.ReturnError("The 'text' field is missing in the 'description' object.");
                    }
                }
                else if (rawDescription == null)
                {
                    LoggingConfigService.ReturnError("Warning: The 'description' field is null or missing.");
                    warningCount++;
                }
                else
                {
                    LoggingConfigService.ReturnError("Warning: The 'description' field is not a valid JSON object.");
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
                            LoggingConfigService.ReturnError($"Error processing company: {truncatedMessage}");
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
                string postedDate = "1970-01-01";
                if (job.GetValueOrDefault("publication_date") is JsonElement dateElement && dateElement.ValueKind == JsonValueKind.String)
                {
                    postedDate = dateElement.GetString();
                    // Optionally, parse and reformat if needed
                    if (DateTime.TryParse(postedDate, out var dt))
                        postedDate = dt.ToString("yyyy-MM-dd");
                }

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
                LoggingConfigService.ReturnInfo($"Skipping invalid job entry: {e.Message}");
            }
        }
        LoggingConfigService.ReturnInfo($"Transformed {transformed.Count} jobs successfully.");
        LoggingConfigService.ReturnInfo($"Number of warnings: {warningCount}");
        return transformed;
    }
    public static void LoadData(List<Dictionary<string, object>> rawJobs)
    {
        LoggingConfigService.ReturnInfo("Processing jobs...");
        try
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            LoggingConfigService.ReturnInfo("Database connection established.");
            LoggingConfigService.ReturnInfo("Starting Load data process...");
            LoggingConfigService.ReturnInfo($"Number of jobs to insert: {rawJobs.Count}");

            // Step 1: Insert raw jobs into raw_table
            foreach (var job in rawJobs)
            {
                //LoggingConfigService.ReturnInfo($"Inserting job_id {job["job_id"]} into raw_table...");
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
                    LoggingConfigService.ReturnError($"MySQL error inserting job_id {job["job_id"]}: {ex.Message}");
                }
            }
            LoggingConfigService.ReturnInfo("All jobs inserted into raw_table.");

            // Step 2: Deduplicate data by inserting distinct rows into Jobs
            var deduplicateCommand = new MySqlCommand(SqlQueries.SqlQuery(5), connection);
            deduplicateCommand.ExecuteNonQuery();
            LoggingConfigService.ReturnInfo("Distinct rows inserted into Jobs.");
        }
        catch (Exception e)
        {
            LoggingConfigService.ReturnError($"Error in ETL process: {e.Message}");
        }
    }

    public static void RunEtlPipeline(string jsonFile)
    {
        LoggingConfigService.ReturnInfo("Starting ETL pipeline Service...");
        var rawData = ExtractData(jsonFile);
        LoggingConfigService.ReturnInfo("Data extracted...");
        var sqlData = TransformData(rawData);
        LoggingConfigService.ReturnInfo("Data transformed...");
        // Call LoadData to handle loading and deduplication
        LoadData(sqlData);
        LoggingConfigService.ReturnInfo("ETL pipeline completed successfully.");
    }
}

}