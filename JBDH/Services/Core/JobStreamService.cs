using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JBDH.Models; // Assuming this is the namespace for your models
using JBDH.Controllers; // Assuming this is the namespace for your controllers
using JBDH.Services.Configuration;

namespace JBDH.Services.Core
{ 
    public class JobStreamService
    {
        private const string TimestampFile = "timestamp.txt";
        private const string JsonFile = @"D:\home\site\data\jobs.json";

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
                client.Timeout = TimeSpan.FromMinutes(10); // Increase timeout for large downloads
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                LoggingConfigService.ReturnInfo($"API Response Content: {content.Substring(0, Math.Min(content.Length, 500))}");
                return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content) ?? new List<Dictionary<string, object>>();
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error fetching job ads: {e.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        public static async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(string url, string? date)
        {
            LoggingConfigService.ReturnInfo($"Fetching job ads from URL: {url} with date: {date}");
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10); // Increase timeout for large downloads

                string fullUrl = url;
                if (!string.IsNullOrEmpty(date))
                {
                    fullUrl = $"{url}?date={Uri.EscapeDataString(date)}";
                }
                LoggingConfigService.ReturnInfo($"Fetching job ads from URL: {fullUrl}");
                var response = await client.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content) ?? new List<Dictionary<string, object>>();
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error fetching job ads: {e.Message}");
                return new List<Dictionary<string, object>>();
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
                var title = job.GetValueOrDefault("headline")?.ToString()?.ToLower() ?? "";
                job.TryGetValue("description", out var descObj);
                var descriptionDict = descObj as Dictionary<string, object>;
                job.TryGetValue("workplace_address", out var locObj);
                var locationDict = locObj as Dictionary<string, object>;
                var jobDescription = descriptionDict?.GetValueOrDefault("text")?.ToString()?.ToLower() ?? "";
                var jobLocation = locationDict?.GetValueOrDefault("municipality")?.ToString()?.ToLower() ?? "";

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
                var fullPath = Path.GetFullPath(JsonFile);
                var dir = Path.GetDirectoryName(fullPath);
                LoggingConfigService.ReturnInfo($"[SaveToJson] Försöker spara jobs.json. Path: {fullPath}");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    LoggingConfigService.ReturnInfo($"[SaveToJson] Skapade mapp: {dir}");
                }
                LoggingConfigService.ReturnInfo($"[SaveToJson] Skriver data till filen...");
                File.WriteAllText(fullPath, jsonData);
                LoggingConfigService.ReturnInfo($"[SaveToJson] Klar! Sparade {data.Count} jobb till {fullPath}");
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    LoggingConfigService.ReturnInfo($"[SaveToJson] jobs.json finns. Storlek: {fileInfo.Length} bytes. Senast ändrad: {fileInfo.LastWriteTimeUtc} UTC");
                }
                else
                {
                    LoggingConfigService.ReturnError($"[SaveToJson] jobs.json kunde INTE hittas direkt efter skrivning!");
                }
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"[SaveToJson] FEL vid sparande av jobs.json: {e.Message}\n{e.StackTrace}");
            }
        }
        public static void DeleteTimestampFile()
        {
            try
            {
                if (File.Exists(TimestampFile))
                {
                    File.Delete(TimestampFile);
                    LoggingConfigService.ReturnInfo($"Deleted timestamp file: {TimestampFile}");
                }
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error deleting timestamp file: {e.Message}");
            }
        }
        public static async Task<List<Dictionary<string, object>>> RunJobStreamAsync(string[] args)
        {
            LoggingConfigService.ReturnInfo("Starting job ad retrieval process...");
            
            // Determine if we should force a full snapshot fetch
            var forceSnapshot = !File.Exists(JsonFile) || new FileInfo(JsonFile).Length == 0;
            var isFirstRun = !File.Exists(TimestampFile);
            var date = isFirstRun ? null : File.ReadAllText(TimestampFile);
            List<Dictionary<string, object>> jobAds;

            if (forceSnapshot || isFirstRun)
            {
                var reason = forceSnapshot ? "jobs.json is missing or empty" : "First run detected";
                LoggingConfigService.ReturnInfo($"{reason}. Fetching full dataset using Snapshot...");
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
            return jobAds ?? new List<Dictionary<string, object>>();
        }
    }
}