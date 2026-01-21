using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class JobSnapshot
{
    [Function("JobSnapshot")]
    public async Task Run([TimerTrigger("* * * * * *")] TimerInfo myTimer, FunctionContext context)
    {
        var log = context.GetLogger("JobSnapshot");

        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        string containerName = "jobs";
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        string blobName = environment == "Development" ? "jobs_test.json" : "jobs.json";
        string content = "Hello from Azure Function! If sats is working live";

        var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();
        var blobClient = containerClient.GetBlobClient(blobName);
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(ms, overwrite: true);

        log.LogInformation("Blob uploaded!");
    }
}