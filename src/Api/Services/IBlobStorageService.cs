namespace DocumentHub.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType);
    Task<Stream> DownloadAsync(string containerName, string blobName);
    Task<bool> DeleteAsync(string containerName, string blobName);
    Task<string> GetBlobUrlAsync(string containerName, string blobName);
    Task<string> GenerateSasUrlAsync(string containerName, string blobName, TimeSpan expiresIn);
}
