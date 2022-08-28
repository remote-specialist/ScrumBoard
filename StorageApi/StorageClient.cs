using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace StorageApi
{
    public class StorageClient : IStorageClient
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public StorageClient(string connectionString, string containerName)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = containerName;
        }

        // Upload
        public async Task UploadAsync(string blobName, string blobData, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            
            await blobClient.UploadAsync(
                BinaryData.FromString(blobData), 
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                });
        }

        // Download
        public async Task<string> DownloadAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var result = await blobClient.DownloadContentAsync();
            return result.Value.Content.ToString();
        }
    }
}