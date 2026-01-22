using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

// This Azure Function uploads a blob to Azure Blob Storage every minute..
public class JobSnapshot
{
    [Function("JobSnapshot")]
    public async Task Run([TimerTrigger("* * * * * *")] TimerInfo myTimer, FunctionContext context)
    {
        var log = context.GetLogger("JobSnapshot");
        log.LogInformation($"Function started at {DateTime.UtcNow:O}");

        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            log.LogError("AzureWebJobsStorage connection string is missing or empty!");
            return;
        }
        log.LogInformation($"Connection string starts with: {connectionString?.Substring(0, Math.Min(30, connectionString.Length))}...");

        string containerName = "jobs";
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        string blobName = environment == "Development" ? "jobs_test.json" : "jobs.json";
        string content = "Hello from Azure Function! If sats is working live";

        try
        {
            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            await blobClient.UploadAsync(ms, overwrite: true);
            log.LogInformation($"Blob '{blobName}' uploaded to container '{containerName}'!");
        }
        catch (Exception ex)
        {
            log.LogError($"Exception during blob upload: {ex.Message}");
        }
    }
}