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
        public static async Task UploadJobsToBlobAsync(string jsonContent, IConfiguration config)
        {
            var accountName = config["AzureBlobStorage:AccountName"];
            var accountKey = config["AzureBlobStorage:AccountKey"];
            var containerName = config["AzureBlobStorage:ContainerName"];
            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient("jobs.json");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
            await blobClient.UploadAsync(stream, overwrite: true);
        }
    }
}
