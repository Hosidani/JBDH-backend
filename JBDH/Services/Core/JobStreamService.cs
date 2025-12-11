using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using JBDH.Services.JobManagement;
using JBDH.Services.Configuration;
using JBDH.Models;
using JBDH.Controllers;

namespace JBDH.Services.Core
{
    public class JobStreamService
    {
        private readonly IConfiguration _configuration;
        private readonly string TimestampFile;
        private readonly string JsonFile;

        private readonly string BaseUrl = "https://jobstream.api.jobtechdev.se";
        private readonly string StreamUrl;
        private readonly string SnapshotUrl;


        public JobStreamService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            TimestampFile = configuration.GetValue<string>("JobStream:TimestampFile") ?? "timestamp.txt";
            JsonFile = configuration.GetValue<string>("JobStream:JsonFile") ?? @"D:\home\site\data\jobs.json";
            BaseUrl = configuration.GetValue<string>("JobStream:BaseUrl") ?? "https://jobstream.api.jobtechdev.se";
            StreamUrl = $"{BaseUrl}/stream";
            SnapshotUrl = $"{BaseUrl}/snapshot";
        }

        // --------------------------------------------------------------------
        // FIXED + CORRECT SNAPSHOT DOWNLOADER
        // --------------------------------------------------------------------
        /// <summary>
        /// Downloads the full job snapshot with pagination and saves all results to jobs.json.
        /// </summary>
        public async Task DownloadFullJobSnapshotAsync(string filePath)
        {
            LoggingConfigService.ReturnInfo($"Starting full job snapshot download to {filePath}...");
            using var client = new HttpClient();
            // Ensure long timeout for Azure background execution
            client.Timeout = TimeSpan.FromMinutes(_configuration.GetValue<int>("JobStream:HttpTimeoutMinutes", 10));

            var allJobs = new List<Dictionary<string, object>>();
            string url = SnapshotUrl;

            while (!string.IsNullOrEmpty(url))
            {
                try
                {
                    LoggingConfigService.ReturnInfo($"Fetching jobs from: {url}");
                    var response = await client.GetStringAsync(url);
                    using var doc = JsonDocument.Parse(response);

                    int pageCount = 0;
                    if (doc.RootElement.TryGetProperty("results", out var resultsEl) && resultsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var job in resultsEl.EnumerateArray())
                        {
                            var jobDict = JsonSerializer.Deserialize<Dictionary<string, object>>(job.GetRawText());
                            if (jobDict != null)
                            {
                                allJobs.Add(jobDict);
                                pageCount++;
                            }
                        }
                    }
                    LoggingConfigService.ReturnInfo($"Fetched {pageCount} jobs on this page. Total so far: {allJobs.Count}");

                    url = doc.RootElement.TryGetProperty("next", out var nextEl)
                        ? nextEl.GetString() ?? ""
                        : "";
                }
                catch (Exception ex)
                {
                    LoggingConfigService.ReturnError($"Error fetching jobs: {ex.Message}");
                    break;
                }
            }

            if (_configuration.GetValue<bool>("UseBlobStorage"))
            {
                await BlobHelper.UploadJobsToBlobAsync(JsonSerializer.Serialize(allJobs), _configuration);
                LoggingConfigService.ReturnInfo($"Saved {allJobs.Count} jobs to Azure Blob Storage");
            }
            else
            {
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(allJobs));
                LoggingConfigService.ReturnInfo($"Saved {allJobs.Count} jobs to local file {filePath}");
            }
        }

        // --------------------------------------------------------------------
        // OLD FETCHERS (only used for incremental stream)
        public async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(string url)
        {
            LoggingConfigService.ReturnInfo($"Fetching job ads from URL: {url}");
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(_configuration.GetValue<int>("JobStream:HttpTimeoutMinutes", 10));

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jobs = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
                return jobs ?? new List<Dictionary<string, object>>();
            }
            catch (HttpRequestException hre)
            {
                LoggingConfigService.ReturnError($"HTTP error: {hre.Message}");
                return new List<Dictionary<string, object>>();
            }
            catch (JsonException je)
            {
                LoggingConfigService.ReturnError($"JSON error: {je.Message}");
                return new List<Dictionary<string, object>>();
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error: {e.Message}");
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(
            string url,
            string? date
        )
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(_configuration.GetValue<int>("JobStream:HttpTimeoutMinutes", 10));

                string fullUrl = string.IsNullOrEmpty(date)
                    ? url
                    : $"{url}?date={Uri.EscapeDataString(date)}";

                var response = await client.GetAsync(fullUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jobs = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(content);
                return jobs ?? new List<Dictionary<string, object>>();
            }
            catch (HttpRequestException hre)
            {
                LoggingConfigService.ReturnError($"HTTP error: {hre.Message}");
                return new List<Dictionary<string, object>>();
            }
            catch (JsonException je)
            {
                LoggingConfigService.ReturnError($"JSON error: {je.Message}");
                return new List<Dictionary<string, object>>();
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
        public void SaveToJson(List<Dictionary<string, object>> data)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(
                    data,
                    new JsonSerializerOptions { WriteIndented = true }
                );

                var fullPath = Path.GetFullPath(JsonFile);
                var dir = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
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
        public async Task<List<Dictionary<string, object>>> RunJobStreamAsync(string[] args)
        {
            bool forceSnapshot =
                !File.Exists(JsonFile) || new FileInfo(JsonFile).Length == 0;
            bool isFirstRun = !File.Exists(TimestampFile);

            List<Dictionary<string, object>> jobAds;

            // Get configuration from DI (for blob/local switch)
            if (forceSnapshot || isFirstRun)
            {
                LoggingConfigService.ReturnInfo("Downloading full snapshot...");
                await DownloadFullJobSnapshotAsync(JsonFile);
                jobAds = EtlPipeline.ExtractData(JsonFile);
                // jobs.json already written by DownloadFullJobSnapshotAsync
            }
            else
            {
                var date = File.ReadAllText(TimestampFile);
                jobAds = await FetchJobAdsAsync(StreamUrl, date);
                SaveToJson(jobAds); // Only write jobs.json for incremental stream
            }

            File.WriteAllText(
                TimestampFile,
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );

            LoggingConfigService.ReturnInfo($"Fetched {jobAds.Count} jobs.");
            return jobAds;
        }
    }
}