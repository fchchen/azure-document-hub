using DocumentHub.Shared.Models;

namespace DocumentHub.Api.Services;

public interface ICosmosDbService
{
    Task<Document> CreateDocumentAsync(Document document);
    Task<Document?> GetDocumentAsync(string id);
    Task<IEnumerable<Document>> GetDocumentsAsync(int page, int pageSize);
    Task<int> GetDocumentCountAsync();
    Task<Document> UpdateDocumentAsync(Document document);
    Task DeleteDocumentAsync(string id);
    Task<IEnumerable<Document>> GetDocumentsByStatusAsync(DocumentStatus status);
}
