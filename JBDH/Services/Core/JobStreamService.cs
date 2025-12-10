using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JBDH.Services.Configuration;
using JBDH.Models;
using JBDH.Controllers;

namespace JBDH.Services.Core
{
    public class JobStreamService
    {
        private const string TimestampFile = "timestamp.txt";
        private const string JsonFile = @"D:\home\site\data\jobs.json";

        private const string BaseUrl = "https://jobstream.api.jobtechdev.se";
        private const string StreamUrl = $"{BaseUrl}/stream";
        private const string SnapshotUrl = $"{BaseUrl}/snapshot";

        private static readonly List<string> Places = new();
        private static readonly List<string> Occupations = new();

        // --------------------------------------------------------------------
        // FIXED + CORRECT SNAPSHOT DOWNLOADER
        // --------------------------------------------------------------------
        /// <summary>
        /// Downloads the full job snapshot with pagination and saves all results to jobs.json.
        /// </summary>
        public static async Task DownloadFullJobSnapshotAsync(string filePath)
        {
            LoggingConfigService.ReturnInfo($"Starting full job snapshot download to {filePath}...");
            using var client = new HttpClient();

            var allJobs = new List<Dictionary<string, object>>();
            string url = SnapshotUrl;

            while (!string.IsNullOrEmpty(url))
            {
                LoggingConfigService.ReturnInfo($"Fetching jobs from: {url}");
                var response = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                int pageCount = 0;
                foreach (var job in doc.RootElement.GetProperty("results").EnumerateArray())
                {
                    allJobs.Add(JsonSerializer.Deserialize<Dictionary<string, object>>(job.GetRawText())!);
                    pageCount++;
                }

                LoggingConfigService.ReturnInfo(
                    $"Fetched {pageCount} jobs on this page. Total so far: {allJobs.Count}"
                );

                url = doc.RootElement.TryGetProperty("next", out var nextEl)
                    ? nextEl.GetString() ?? ""
                    : "";
            }

            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(allJobs));
            LoggingConfigService.ReturnInfo($"Saved {allJobs.Count} jobs to {filePath}");
        }

        // --------------------------------------------------------------------
        // OLD FETCHERS (only used for incremental stream)
        // --------------------------------------------------------------------
        public static async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(string url)
        {
            LoggingConfigService.ReturnInfo($"Fetching job ads from URL: {url}");
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content)
                    ?? new List<Dictionary<string, object>>();
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error: {e.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        public static async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(
            string url,
            string? date
        )
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10);

                string fullUrl = string.IsNullOrEmpty(date)
                    ? url
                    : $"{url}?date={Uri.EscapeDataString(date)}";

                var response = await client.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content)
                    ?? new List<Dictionary<string, object>>();
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error: {e.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        // --------------------------------------------------------------------
        // SAVE TO JSON (unchanged)
        // --------------------------------------------------------------------
        public static void SaveToJson(List<Dictionary<string, object>> data)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(
                    data,
                    new JsonSerializerOptions { WriteIndented = true }
                );

                var fullPath = Path.GetFullPath(JsonFile);
                var dir = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(fullPath, jsonData);
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"SaveToJson error: {e.Message}");
            }
        }

        // --------------------------------------------------------------------
        // MAIN ENTRY POINT
        // --------------------------------------------------------------------
        public static async Task<List<Dictionary<string, object>>> RunJobStreamAsync(string[] args)
        {
            bool forceSnapshot =
                !File.Exists(JsonFile) || new FileInfo(JsonFile).Length == 0;
            bool isFirstRun = !File.Exists(TimestampFile);

            List<Dictionary<string, object>> jobAds;

            if (forceSnapshot || isFirstRun)
            {
                LoggingConfigService.ReturnInfo("Downloading full snapshot...");

                await DownloadFullJobSnapshotAsync(JsonFile);
                jobAds = EtlPipeline.ExtractData(JsonFile);
            }
            else
            {
                var date = File.ReadAllText(TimestampFile);
                jobAds = await FetchJobAdsAsync(StreamUrl, date);
            }

            SaveToJson(jobAds);
            File.WriteAllText(
                TimestampFile,
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );

            LoggingConfigService.ReturnInfo($"Fetched {jobAds.Count} jobs.");
            return jobAds;
        }
    }
}