using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JBDH.Models; // Assuming this is the namespace for your models
using JBDH.Controllers; // Assuming this is the namespace for your controllers

namespace JBDH.Services
{ 
class JobStreamService
    {
        private const string DbTableName = "testing_purposes";
        private const string DbFileName = "testing_purposes.db";
        private const string TimestampFile = "timestamp2.txt";
        private const string JsonFile = "jobs.json";

        private const string BaseUrl = "https://jobstream.api.jobtechdev.se";
        private const string StreamUrl = $"{BaseUrl}/stream";
        private const string SnapshotUrl = $"{BaseUrl}/snapshot";

        private static readonly List<string> Places = new List<string>(); // Add place IDs if needed
        private static readonly List<string> Occupations = new List<string>(); // Add occupation IDs if needed

        /// <summary>
        /// Fetches job ads from the given API URL.
        /// </summary>
        /// <param name="url">The API endpoint to fetch job ads from.</param>
        /// <returns>A list of job ads as raw JSON objects.</returns>
        public static async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(string url)
        {
            LoggingConfigService.ReturnInfo($"Fetching job ads from URL: {url}");
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
                LoggingConfigService.ReturnError($"Error fetching job ads: {e.Message}");
                return null;
            }
        }

        public static async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(string url, string date)
        {
            LoggingConfigService.ReturnInfo($"Fetching job ads from URL: {url} with date: {date}");
            try
            {
                using var client = new HttpClient();

                // Add the date parameter to the query string
                var fullUrl = $"{url}?date={Uri.EscapeDataString(date)}";
                LoggingConfigService.ReturnInfo($"Fetching job ads from URL: {fullUrl}");
                var response = await client.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error fetching job ads: {e.Message}");
                return null;
            }
        }

        public static List<Dictionary<string, object>> FilterJobs(List<Dictionary<string, object>> jobAds, string description = "", string location = "")
        {
            var filteredJobs = new List<Dictionary<string, object>>();

            if (jobAds == null)
            {
                return filteredJobs; // Return an empty list if jobAds is null
            }

            foreach (var job in jobAds)
            {
                var title = job.GetValueOrDefault("headline", "").ToString().ToLower();
                var descriptionDict = job.GetValueOrDefault("description", null) as Dictionary<string, object>;
                var jobDescription = descriptionDict?.GetValueOrDefault("text", "").ToString().ToLower() ?? "";
                var locationDict = job.GetValueOrDefault("workplace_address", null) as Dictionary<string, object>;
                var jobLocation = locationDict?.GetValueOrDefault("municipality", "").ToString().ToLower() ?? "";

                if ((string.IsNullOrEmpty(description) || title.Contains(description.ToLower()) || jobDescription.Contains(description.ToLower())) &&
                    (string.IsNullOrEmpty(location) || jobLocation.Contains(location.ToLower())))
                {
                    filteredJobs.Add(job);
                }
            }

            return filteredJobs;
        }

        
        public static void SaveToJson(List<Dictionary<string, object>> data)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(JsonFile, jsonData);
                LoggingConfigService.ReturnInfo($"Saving data to {JsonFile}...");
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error saving JSON file: {e.Message}");
            }
        }
        public static async Task Main(string[] args)
        {
            LoggingConfigService.ReturnInfo("Starting job ad retrieval process...");
            // Check if this is the first run or an update
            var isFirstRun = !File.Exists("timestamp.txt"); // Use a file to track the last fetch timestamp
            var date = isFirstRun ? null : File.ReadAllText("timestamp.txt");
            List<Dictionary<string, object>> jobAds;

            if (isFirstRun)
            {
                LoggingConfigService.ReturnInfo("First run detected. Fetching full dataset using Snapshot...");
                jobAds = await FetchJobAdsAsync(SnapshotUrl);
            }
            else
            {
                LoggingConfigService.ReturnInfo($"Last fetch date: {date}");
                LoggingConfigService.ReturnInfo("Fetching incremental updates using Stream...");
                jobAds = await FetchJobAdsAsync(StreamUrl, date);
            }

            if (jobAds != null)
            {
                // Save the fetched job ads directly to the JSON file
                SaveToJson(jobAds);

                // Update the timestamp file with the current date
                File.WriteAllText("timestamp.txt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                LoggingConfigService.ReturnInfo($"Updated timestamp to {DateTime.UtcNow}");
                LoggingConfigService.ReturnInfo($"Fetched {jobAds.Count} job ads from the API.");
            }
            else
            {
                LoggingConfigService.ReturnError("Failed to fetch job ads from the API.");
            }
            LoggingConfigService.ReturnInfo("Job ads retrieval process completed.");
        }
    }
}