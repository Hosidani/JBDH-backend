using Azure.Storage.Blobs;
using Azure.Identity;
using System.Text;


namespace JBDH.Services.JobManagement
{
    public static class BlobHelper
    {
        /// <summary>
        /// Serializes the job list as pretty-printed JSON and uploads it to Azure Blob Storage.
        /// </summary>
        /// <param name="jobs">The list of jobs to upload.</param>
        /// <param name="config">The configuration for blob storage.</param>
        public static async Task UploadJobsToBlobAsync(List<Dictionary<string, object>> jobs, IConfiguration config)
        {
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string jsonContent = System.Text.Json.JsonSerializer.Serialize(jobs, options);
            await UploadJobsToBlobAsync(jsonContent, config);
        }

        public static async Task UploadStreamToBlobAsync(Stream source, IConfiguration config)
        {
            var accountName = config["AzureBlobStorage:AccountName"];
            var containerName = config["AzureBlobStorage:ContainerName"];
            var blobFileName = config["AzureBlobStorage:BlobFileName"] ?? "jobs.json";

            var blobUri = new Uri($"https://{accountName}.blob.core.windows.net");
            var blobServiceClient = new BlobServiceClient(blobUri, new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobFileName);
            await blobClient.UploadAsync(source, overwrite: true);
        }
        public static async Task UploadJobsToBlobAsync(string jsonContent, IConfiguration config)
        {
            var accountName = config["AzureBlobStorage:AccountName"];
            var containerName = config["AzureBlobStorage:ContainerName"];
            var blobFileName = config["AzureBlobStorage:BlobFileName"] ?? "jobs.json";

            var blobUri = new Uri($"https://{accountName}.blob.core.windows.net");
            var blobServiceClient = new BlobServiceClient(blobUri, new DefaultAzureCredential());
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobFileName);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }
}
