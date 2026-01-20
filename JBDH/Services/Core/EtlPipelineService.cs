using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using JBDH.Services.JobManagement;
using JBDH.Models.Core;
using JBDH.Services.Configuration;
using MySql.Data.MySqlClient;
using JBDH.Models.Job;
namespace JBDH.Services.Core
{
    public class EtlPipeline
    {
        public static List<Dictionary<string, object>> TransformData(List<Dictionary<string, object>> rawJobs)
        {
            LoggingConfigService.ReturnInfo($"Transforming {rawJobs.Count} jobs...");
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

            int totalMissing = missingDescriptionCount + missingTitleCount + missingEmployerCount + missingLocationCount;
            if (totalMissing > 0)
            {
                LoggingConfigService.ReturnInfo(
                    $"Missing fields: Description={missingDescriptionCount}, Title={missingTitleCount}, Employer={missingEmployerCount}, Location={missingLocationCount} (Total: {totalMissing})"
                );
            }

            foreach (var job in rawJobs)
            {
                // Skip jobs that are marked as removed
                if (job.TryGetValue("removed", out var removedVal) && removedVal is bool removed && removed)
                    continue;

                try
                {
                    int jobId = -1;
                    if (job.TryGetValue("id", out var idValue) && idValue != null && int.TryParse(idValue.ToString(), out var parsedId))
                        jobId = parsedId;

                    var rawTitle = job.GetValueOrDefault("headline")?.ToString() ?? "Unknown Title";
                    var title = rawTitle != null ? DecodeUnicodeString(rawTitle) : "Unknown Title";

                    static string DecodeUnicodeString(string input)
                    {
                        return System.Text.RegularExpressions.Regex.Unescape(input);
                    }

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
                                description = description.Replace("\n", " ").Replace("\r", " ").Trim();
                            }
                            catch
                            {
                                description = "Invalid Description Format";
                                warningCount++;
                            }
                        }
                        else
                        {
                            warningCount++;
                        }
                    }
                    else if (rawDescription == null)
                    {
                        warningCount++;
                    }
                    else
                    {
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
                                company = company.Replace("\n", " ").Replace("\r", " ").Trim();
                            }
                            catch
                            {
                                company = "Invalid company name";
                                warningCount++;
                            }
                        }
                    }

                    string location = "Unknown Location";
                    if (job.GetValueOrDefault("workplace_address") is JsonElement addressElement && addressElement.ValueKind == JsonValueKind.Object)
                    {
                        if (addressElement.TryGetProperty("municipality", out JsonElement municipalityElement))
                        {
                            location = DecodeUnicodeString(municipalityElement.GetString() ?? "Unknown Location");
                        }
                    }

                    string postedDate = "1970-01-01";
                    if (job.GetValueOrDefault("publication_date") is JsonElement dateElement && dateElement.ValueKind == JsonValueKind.String)
                    {
                        postedDate = dateElement.GetString() ?? "1970-01-01";
                        if (DateTime.TryParse(postedDate, out var dt))
                            postedDate = dt.ToString("yyyy-MM-dd");
                    }

                    var webpageUrl = job.GetValueOrDefault("webpage_url")?.ToString() ?? "Unknown URL";

                    transformed.Add(new Dictionary<string, object>
                    {
                        { "job_id", jobId },
                        { "title", title ?? "Unknown Title" },
                        { "description", description ?? "No Description" },
                        { "company", company ?? "Unknown Company" },
                        { "location", location ?? "Unknown Location" },
                        { "PostedDate", postedDate ?? "1970-01-01" },
                        { "WebpageUrl", webpageUrl ?? "Unknown URL" }
                    });
                }
                catch (Exception e)
                {
                    LoggingConfigService.ReturnError($"Skipping invalid job entry: {e.Message}");
                }
            }

            LoggingConfigService.ReturnInfo($"Transformed {transformed.Count} jobs. Warnings: {warningCount}");
            return transformed;
        }

        public static void LoadData(List<Dictionary<string, object>> rawJobs, string connectionString)
        {
            LoggingConfigService.ReturnInfo($"Loading {rawJobs.Count} jobs into database...");
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                foreach (var job in rawJobs)
                {
                    var command = new MySqlCommand(SqlQueries.SqlQuery(100), connection);
                    command.Parameters.AddWithValue("@job_id", job["job_id"]);
                    command.Parameters.AddWithValue("@title", job["title"]);
                    command.Parameters.AddWithValue("@description", job["description"]);
                    command.Parameters.AddWithValue("@company", job["company"]);
                    command.Parameters.AddWithValue("@location", job["location"]);
                    command.Parameters.AddWithValue("@PostedDate", job["PostedDate"]);
                    command.Parameters.AddWithValue("@WebpageUrl", job["WebpageUrl"]);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (MySqlException)
                    {
                        // Loggning borttagen, endast felhantering
                    }
                }
                LoggingConfigService.ReturnInfo("All jobs inserted into rawjobs.");

                var deduplicateCommand = new MySqlCommand(SqlQueries.SqlQuery(101), connection);
                deduplicateCommand.ExecuteNonQuery();
                LoggingConfigService.ReturnInfo("Deduplication complete.");
            }
            catch (Exception e)
            {
                LoggingConfigService.ReturnError($"Error in ETL process: {e.Message}");
            }
        }

        public static void LoadDataEf(List<Dictionary<string, object>> jobs, ApplicationDbContext db)
        {
            foreach (var job in jobs)
            {
                int jobId = Convert.ToInt32(job["job_id"]);
                if (!db.RawJobs.Any(j => j.JobId == jobId))
                {
                    db.RawJobs.Add(new RawJob
                    {
                        JobId = jobId,
                        Title = job["title"].ToString(),
                        Description = job["description"].ToString(),
                        Company = job["company"].ToString(),
                        Location = job["location"].ToString(),
                        PostedDate = DateTime.Parse(job["PostedDate"]?.ToString() ?? "1970-01-01"),
                        WebpageUrl = job["WebpageUrl"].ToString()
                    });
                }
            }
            db.SaveChanges();
        }

        public static async Task RunEtlPipelineAsync(List<Dictionary<string, object>> rawData, ApplicationDbContext dbContext, IConfiguration config)
        {
            LoggingConfigService.ReturnInfo("Starting ETL pipeline...");
            var transformedData = TransformData(rawData);
            // Upload pretty-printed jobs to blob storage
            await BlobHelper.UploadJobsToBlobAsync(rawData, config);
            LoadDataEf(transformedData, dbContext);
            LoggingConfigService.ReturnInfo("ETL pipeline completed.");
        }
    }
}