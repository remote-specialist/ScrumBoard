namespace StorageApi
{
    public interface IStorageClient
    {
        Task UploadAsync(string blobName, string blobData, string contentType);
        Task<string> DownloadAsync(string blobName);
    }
}
