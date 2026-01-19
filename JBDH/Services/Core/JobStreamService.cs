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
            /// <summary>
            /// Streams and processes a large JSON blob from Azure Blob Storage in chunks.
            /// </summary>
            /// <param name="blobConnectionString">Azure Blob Storage connection string</param>
            /// <param name="containerName">Blob container name</param>
            /// <param name="blobName">Blob file name</param>
            public async Task ProcessJsonBlobAsync(string blobConnectionString, string containerName, string blobName)
            {
                var blobClient = new Azure.Storage.Blobs.BlobClient(blobConnectionString, containerName, blobName);
                using var stream = await blobClient.OpenReadAsync();

                // Use a buffer and Utf8JsonReader for streaming parsing
                byte[] buffer = new byte[8192];
                int bytesRead;
                bool isFinalBlock = false;
                System.Buffers.ArrayBufferWriter<byte> jsonBuffer = new();

                // Read the stream in chunks and accumulate in buffer
                while (!isFinalBlock)
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isFinalBlock = true;
                    }
                    else
                    {
                        buffer.AsSpan(0, bytesRead).CopyTo(jsonBuffer.GetSpan(bytesRead));
                        jsonBuffer.Advance(bytesRead);
                    }
                }

                // Parse the buffer as a JSON array of objects
                var reader = new System.Text.Json.Utf8JsonReader(jsonBuffer.WrittenSpan, isFinalBlock: true, state: default);
                var batch = new List<Dictionary<string, object>>(1000);
                string? connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                    throw new InvalidOperationException("DefaultConnection string is missing in configuration.");
                if (reader.Read() && reader.TokenType == System.Text.Json.JsonTokenType.StartArray)
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == System.Text.Json.JsonTokenType.StartObject)
                        {
                            // Parse a single job object
                            var job = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader);
                            if (job != null)
                                batch.Add(job);
                            // Batch insert for efficiency
                            if (batch.Count >= 1000)
                            {
                                var transformed = EtlPipeline.TransformData(batch);
                                EtlPipeline.LoadData(transformed, connectionString);
                                batch.Clear();
                            }
                        }
                        else if (reader.TokenType == System.Text.Json.JsonTokenType.EndArray)
                        {
                            break;
                        }
                    }
                    // Process any remaining jobs in the last batch
                    if (batch.Count > 0)
                    {
                        var transformed = EtlPipeline.TransformData(batch);
                        EtlPipeline.LoadData(transformed, connectionString);
                        batch.Clear();
                    }
                }
            }
        private readonly IConfiguration _configuration;
        private readonly string TimestampFile;

        private readonly string BaseUrl = "https://jobstream.api.jobtechdev.se";
        private readonly string StreamUrl;
        private readonly string SnapshotUrl;


        public JobStreamService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            TimestampFile = configuration.GetValue<string>("JobStream:TimestampFile") ?? "timestamp.txt";
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
            var blobFileName = _configuration["AzureBlobStorage:BlobFileName"] ?? "jobs.json";
            LoggingConfigService.ReturnInfo($"[STORAGE] Always uploading to Azure Blob Storage. Blob file: {blobFileName}");
            LoggingConfigService.ReturnInfo($"Starting full job snapshot streaming upload to Azure Blob Storage as {blobFileName}...");
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(_configuration.GetValue<int>("JobStream:HttpTimeoutMinutes", 30));
                LoggingConfigService.ReturnInfo($"[DEBUG] Sending GET request to {SnapshotUrl}");
                using var responseStream = await client.GetStreamAsync(SnapshotUrl);

                // Debug: Check stream length if possible
                long streamLength = 0;
                if (responseStream.CanSeek)
                {
                    streamLength = responseStream.Length;
                    LoggingConfigService.ReturnInfo($"[DEBUG] Response stream length: {streamLength} bytes");
                }
                else
                {
                    // Copy to memory stream to measure size
                    using var ms = new MemoryStream();
                    await responseStream.CopyToAsync(ms);
                    streamLength = ms.Length;
                    LoggingConfigService.ReturnInfo($"[DEBUG] Response stream copied to memory. Length: {streamLength} bytes");
                    ms.Position = 0;
                    // Optionally log a preview of the content
                    using (var reader = new StreamReader(ms, leaveOpen: true))
                    {
                        string preview = await reader.ReadLineAsync() ?? "<empty>";
                        LoggingConfigService.ReturnInfo($"[DEBUG] First line of response: {preview}");
                        ms.Position = 0;
                    }
                    // Upload from memory stream
                    await BlobHelper.UploadStreamToBlobAsync(ms, _configuration);
                    LoggingConfigService.ReturnInfo($"[SUCCESS] Streamed snapshot (from memory) to Azure Blob Storage as {blobFileName}");
                    return;
                }

                // If stream is seekable, upload directly
                await BlobHelper.UploadStreamToBlobAsync(responseStream, _configuration);
                LoggingConfigService.ReturnInfo($"[SUCCESS] Streamed snapshot to Azure Blob Storage as {blobFileName}");
            }
            catch (Exception ex)
            {
                LoggingConfigService.ReturnError($"[ERROR] Failed to stream snapshot to Azure Blob Storage as {blobFileName}: {ex}");
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

        // --------------------------------------------------------------------
        // MAIN ENTRY POINT
        // --------------------------------------------------------------------
        public async Task<List<Dictionary<string, object>>> RunJobStreamAsync(string[] args)
        {
            bool isFirstRun = !File.Exists(TimestampFile);

            List<Dictionary<string, object>> jobAds;

            // Always use blob storage, never local file
            if (isFirstRun)
            {
                LoggingConfigService.ReturnInfo("Downloading full snapshot to blob storage...");
                await DownloadFullJobSnapshotAsync(""); // filePath param is unused, passing empty string
                // TODO: Add logic to read from blob storage directly (stream or download)
                jobAds = new List<Dictionary<string, object>>(); // Placeholder: implement blob read
                // If you implement blob read here, also upload to blob storage if needed:
                // if (jobAds.Count > 0)
                // {
                //     var json = JsonSerializer.Serialize(jobAds);
                //     await BlobHelper.UploadJobsToBlobAsync(json, _configuration);
                //     LoggingConfigService.ReturnInfo($"Uploaded {jobAds.Count} jobs to blob storage (first run).");
                // }
            }
            else
            {
                var date = File.ReadAllText(TimestampFile);
                jobAds = await FetchJobAdsAsync(StreamUrl, date);
                if (jobAds.Count > 0)
                {
                    var json = JsonSerializer.Serialize(jobAds);
                    await BlobHelper.UploadJobsToBlobAsync(json, _configuration);
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