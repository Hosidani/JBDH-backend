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
        private readonly ILogger<JobStreamService> _logger;
        private readonly string TimestampFile;

        private readonly string BaseUrl = "https://jobstream.api.jobtechdev.se";
        private readonly string StreamUrl;
        private readonly string SnapshotUrl;


        public JobStreamService(IConfiguration configuration, ILogger<JobStreamService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            TimestampFile = configuration.GetValue<string>("JobStream:TimestampFile") ?? "timestamp.txt";
            BaseUrl = configuration.GetValue<string>("JobStream:BaseUrl") ?? "https://jobstream.api.jobtechdev.se";
            StreamUrl = $"{BaseUrl}/stream";
            SnapshotUrl = $"{BaseUrl}/snapshot";
        }

        public async Task DownloadFullJobSnapshotAsync(string filePath)
            // Hämtar hela job-snapshot från API och laddar upp till blob storage
        {
            var blobFileName = _configuration["AzureBlobStorage:BlobFileName"] ?? "jobs.json";
            LoggingConfigService.ReturnInfo($"[STORAGE] Always uploading to Azure Blob Storage. Blob file: {blobFileName}");
            LoggingConfigService.ReturnInfo($"Starting full job snapshot streaming upload to Azure Blob Storage as {blobFileName}...");

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(_configuration.GetValue<int>("JobStream:HttpTimeoutMinutes", 30));
                LoggingConfigService.ReturnInfo($"[DEBUG] Sending GET request to {SnapshotUrl}");

                // Log before request
                var sw = System.Diagnostics.Stopwatch.StartNew();
                HttpResponseMessage? response = null;
                try
                {
                    response = await client.GetAsync(SnapshotUrl, HttpCompletionOption.ResponseHeadersRead);
                }
                catch (Exception ex)
                {
                    LoggingConfigService.ReturnError($"[ERROR] HTTP GET failed: {ex.Message}");
                    throw;
                }
                sw.Stop();
                LoggingConfigService.ReturnInfo($"[DEBUG] HTTP GET completed in {sw.ElapsedMilliseconds} ms. Status: {(response != null ? ((int)response.StatusCode + " " + response.StatusCode) : "null")}");

                if (response == null)
                {
                    LoggingConfigService.ReturnError("[ERROR] HTTP response is null!");
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    LoggingConfigService.ReturnError($"[ERROR] HTTP GET failed. Status: {(int)response.StatusCode} {response.StatusCode}");
                    return;
                }

                var contentLength = response.Content.Headers.ContentLength;
                LoggingConfigService.ReturnInfo($"[DEBUG] HTTP response Content-Length: {contentLength}");

                using var responseStream = await response.Content.ReadAsStreamAsync();

                // Stream directly to Azure Blob Storage (no buffering in MemoryStream)
                await BlobHelper.UploadStreamToBlobAsync(responseStream, _configuration);

                LoggingConfigService.ReturnInfo($"[SUCCESS] Streamed snapshot to Azure Blob Storage as {blobFileName}");
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"[ERROR] Failed to stream snapshot to Azure Blob Storage as {blobFileName}: {ex}");
            }
        }

        public async Task<List<Dictionary<string, object>>> FetchJobAdsAsync(
                // Hämtar job-annonser inkrementellt från API (med datumfilter)
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

        public async Task<List<Dictionary<string, object>>> RunJobStreamAsync(string[] args)
        {
            bool isFirstRun = !File.Exists(TimestampFile);

            List<Dictionary<string, object>> jobAds;

            // Always use blob storage, never local file
            if (isFirstRun)
            {
                LoggingConfigService.ReturnInfo("Downloading full snapshot to blob storage...");
                await DownloadFullJobSnapshotAsync(""); // File path not needed for blob upload
                jobAds = new List<Dictionary<string, object>>(); // Placeholder: implement blob read
                LoggingConfigService.ReturnInfo("Uploaded full snapshot to blob storage.");
            }
            else
            {
                // TODO: Om mängden data är stor, batcha eller streama jobben för att undvika minnesproblem vid inkrementell uppdatering.
                var date = File.ReadAllText(TimestampFile);
                jobAds = await FetchJobAdsAsync(StreamUrl, date);
                if (jobAds.Count > 0)
                {
                    var json = JsonSerializer.Serialize(jobAds);
                    _logger.LogInformation("Json size: {len}", json.Length);
                    _logger.LogInformation("Jobs count: {count}", jobAds.Count);
                    LoggingConfigService.ReturnInfo($"Jobs count: {jobAds.Count}");
                    await BlobHelper.UploadJobsToBlobAsync(jobAds, _configuration);
                    LoggingConfigService.ReturnInfo($"Uploaded {jobAds.Count} jobs to blob storage (incremental update).");
                }
            }

            File.WriteAllText(
                TimestampFile,
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );

            LoggingConfigService.ReturnInfo($"Fetched {jobAds.Count} jobs.");
            return jobAds;
        }

        public async Task<bool> TestBlobStorageConnectionAsync()
        {
            try
            {
                var testJson = "{\"test\": true}";
                await BlobHelper.UploadJobsToBlobAsync(testJson, _configuration);
                LoggingConfigService.ReturnInfo("Successfully uploaded test JSON to Blob Storage as jobs_test.json.");
                return true;
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"Blob Storage connection or upload failed: {ex.Message}");
                return false;
            }
        }
    }
}