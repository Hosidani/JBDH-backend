using Azure.Storage.Blobs;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using JBDH.Services.JobManagement;

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

            public static async Task UploadStreamToBlobAsync(Stream inputStream, IConfiguration config)
            {
                var accountName = config["AzureBlobStorage:AccountName"];
                var accountKey = config["AzureBlobStorage:AccountKey"];
                var containerName = config["AzureBlobStorage:ContainerName"];
                var blobFileName = config["AzureBlobStorage:BlobFileName"] ?? "jobs.json";
                var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(blobFileName);
                await blobClient.UploadAsync(inputStream, overwrite: true);
            }
        public static async Task UploadJobsToBlobAsync(string jsonContent, IConfiguration config)
        {
            var accountName = config["AzureBlobStorage:AccountName"];
            var accountKey = config["AzureBlobStorage:AccountKey"];
            var containerName = config["AzureBlobStorage:ContainerName"];
            var blobFileName = config["AzureBlobStorage:BlobFileName"] ?? "jobs.json";
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobFileName);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }
}
