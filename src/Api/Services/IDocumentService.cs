using DocumentHub.Shared.DTOs;
using DocumentHub.Shared.Models;

namespace DocumentHub.Api.Services;

public interface IDocumentService
{
    Task<DocumentUploadResponse> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, string uploadedBy);
    Task<DocumentResponse?> GetDocumentAsync(string id);
    Task<DocumentListResponse> GetDocumentsAsync(int page, int pageSize);
    Task<Stream> DownloadDocumentAsync(string id);
    Task<string> GetDownloadUrlAsync(string id, TimeSpan? expiresIn = null);
    Task DeleteDocumentAsync(string id);
}
