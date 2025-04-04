using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JBDH.Models; // Assuming this is the namespace for your models
using JBDH.Controllers; // Assuming this is the namespace for your controllers

namespace JBDH.Controllers; // Assuming this is the namespace for your controller

class SearchApi
{
    private const string DbTableName = "jobads";
    private const string DbFileName = "jobads_database_20220127.db";
    private const string TimestampFile = "timestamp2.txt";
    private const string JsonFile = "systemutvecklare_jobs.json";
 
    private const string BaseUrl = "https://jobstream.api.jobtechdev.se";
    private const string StreamUrl = $"{BaseUrl}/stream";
    private const string SnapshotUrl = $"{BaseUrl}/snapshot";
 
    private static readonly List<string> Places = new List<string>(); // Add place IDs if needed
    private static readonly List<string> Occupations = new List<string>(); // Add occupation IDs if needed
 
    public static async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(string url, Dictionary<string, string> parameters = null)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
 
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
        }
        catch (Exception e)
        {
            LoggingConfig.ReturnError($"Request failed: {e.Message}");
            return null;
        }
    }
 
    public static List<Dictionary<string, object>> FilterSystemutvecklareJobs(List<Dictionary<string, object>> jobAds)
    {
        var filteredJobs = new List<Dictionary<string, object>>();
        foreach (var job in jobAds)
        {
            var title = job.GetValueOrDefault("headline", "").ToString().ToLower();
            var descriptionDict = job.GetValueOrDefault("description", null) as Dictionary<string, object>;
            var description = descriptionDict?.GetValueOrDefault("text", "").ToString().ToLower() ?? "";
 
            if (title.Contains("systemutvecklare") || description.Contains("systemutvecklare"))
            {
                filteredJobs.Add(job);
            }
        }
        return filteredJobs;
    }
 
    public static void PrintJobDetails(Dictionary<string, object> job)
    {
        Console.WriteLine($"Title: {job.GetValueOrDefault("headline", "N/A")}");
        Console.WriteLine($"Company: {((Dictionary<string, object>)job.GetValueOrDefault("employer", new Dictionary<string, object>()))
            .GetValueOrDefault("name", "N/A")}");
        Console.WriteLine($"Location: {((Dictionary<string, object>)job.GetValueOrDefault("workplace_address", new Dictionary<string, object>()))
            .GetValueOrDefault("municipality", "N/A")}");
        Console.WriteLine($"Application deadline: {job.GetValueOrDefault("application_deadline", "N/A")}");
        var descriptionDict = job.GetValueOrDefault("description", null) as Dictionary<string, object>;
        var description = descriptionDict?.GetValueOrDefault("text", "")?.ToString();
        Console.WriteLine($"Description: {description?.Substring(0, Math.Min(200, description.Length))}...");
        Console.WriteLine(new string('-', 50));
    }
 
    public static void SaveToJson(List<Dictionary<string, object>> data, string filename)
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, jsonData);
            LoggingConfig.ReturnInfo($"Successfully saved data to {filename}");
        }
        catch (Exception e)
        {
            LoggingConfig.ReturnError($"Failed to save JSON file: {e.Message}");
        }
    }
 
    public static async Task Main(string[] args)
    {
        LoggingConfig.ReturnInfo("Starting job ad retrieval process...");
 
        var parameters = new Dictionary<string, string>();
        if (Places.Count > 0)
        {
            parameters["places"] = string.Join(",", Places);
        }
        if (Occupations.Count > 0)
        {
            parameters["occupations"] = string.Join(",", Occupations);
        }
 
        var jobAds = await FetchJobAdsAsync(StreamUrl, parameters) ?? await FetchJobAdsAsync(SnapshotUrl, parameters);
 
        if (jobAds != null)
        {
            var systemutvecklareJobs = FilterSystemutvecklareJobs(jobAds);
 
            if (systemutvecklareJobs.Count > 0)
            {
                SaveToJson(systemutvecklareJobs, JsonFile);
                LoggingConfig.ReturnInfo($"Saved {systemutvecklareJobs.Count} 'systemutvecklare' jobs to {JsonFile}");
            }
            else
            {
                LoggingConfig.ReturnWarning("No 'systemutvecklare' jobs found in the data.");
            }
        }
        else
        {
            LoggingConfig.ReturnError("Failed to retrieve job ads from both sources.");
        }
 
        LoggingConfig.ReturnInfo("Process completed.");
    }
}