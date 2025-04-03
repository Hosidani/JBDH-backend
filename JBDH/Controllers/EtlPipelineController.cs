using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MySql.Data.MySqlClient;
using JBDH.Models; // Assuming this is the namespace for your models
 
 namespace JBDH.Controllers; // Assuming this is the namespace for your controller

class EtlPipeline
{
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
            return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonData);
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
                    { "job_id", job.GetValueOrDefault("id") },
                    { "title", job.GetValueOrDefault("headline", "Unknown Title") },
                    { "description", ((Dictionary<string, object>)job.GetValueOrDefault("description", new Dictionary<string, object>())).GetValueOrDefault("text", "No Description") },
                    { "company", ((Dictionary<string, object>)job.GetValueOrDefault("employer", new Dictionary<string, object>())).GetValueOrDefault("name", "Unknown Company") },
                    { "location", ((Dictionary<string, object>)job.GetValueOrDefault("workplace_address", new Dictionary<string, object>())).GetValueOrDefault("municipality", "Unknown Location") },
                    { "posted_date", job.GetValueOrDefault("publication_date") },
                    { "webpage_url", job.GetValueOrDefault("webpage_url") }
                });
            }
            catch (Exception e)
            {
                LoggingConfig.ReturnInfo($"Skipping invalid job entry: {e.Message}");
            }
        }
        return transformed;
    }
 
    public static void LoadData(List<Dictionary<string, object>> transformedJobs)
    {
        using var conn = new MySqlConnection(Settings.SetupConnection());
        conn.Open();
        using var cmd = conn.CreateCommand();
 
        cmd.CommandText = SqlQueries.SqlQuery(1);
        cmd.ExecuteNonQuery();
 
        cmd.CommandText = SqlQueries.SqlQuery(2);
        cmd.ExecuteNonQuery();
 
        foreach (var job in transformedJobs)
        {
            cmd.CommandText = SqlQueries.SqlQuery(3);
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@job_id", job["job_id"]);
            cmd.Parameters.AddWithValue("@title", job["title"]);
            cmd.Parameters.AddWithValue("@description", job["description"]);
            cmd.Parameters.AddWithValue("@company", job["company"]);
            cmd.Parameters.AddWithValue("@location", job["location"]);
            cmd.Parameters.AddWithValue("@posted_date", job["posted_date"]);
            cmd.Parameters.AddWithValue("@webpage_url", job["webpage_url"]);
            cmd.ExecuteNonQuery();
        }
 
        cmd.CommandText = SqlQueries.SqlQuery(5);
        cmd.ExecuteNonQuery();
 
        cmd.CommandText = SqlQueries.SqlQuery(6);
        cmd.ExecuteNonQuery();
 
        conn.Close();
    }
 
    public static void RunEtlPipeline(string jsonFile)
    {
        LoggingConfig.ReturnInfo("Starting ETL pipeline...\nExtracting raw data from JSON file...");
        var rawData = ExtractData(jsonFile);
        LoggingConfig.ReturnInfo("Data extracted...\nTransforming data...");
        var sqlData = TransformData(rawData);
        LoggingConfig.ReturnInfo("Data transformed...\nLoading data to database table...");
        LoadData(sqlData);
    }
}
 