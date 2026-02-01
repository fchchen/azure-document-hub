using DocumentHub.Shared.Constants;
using DocumentHub.Shared.DTOs;
using DocumentHub.Shared.Models;

namespace DocumentHub.Api.Services;

public class DocumentService : IDocumentService
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ICosmosDbService _cosmosDb;
    private readonly IQueueService _queueService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IBlobStorageService blobStorage,
        ICosmosDbService cosmosDb,
        IQueueService queueService,
        ILogger<DocumentService> logger)
    {
        _blobStorage = blobStorage;
        _cosmosDb = cosmosDb;
        _queueService = queueService;
        _logger = logger;
    }

    public async Task<DocumentUploadResponse> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string uploadedBy)
    {
        var document = new Document
        {
            OriginalFileName = fileName,
            FileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}",
            ContentType = contentType,
            FileSize = fileStream.Length,
            UploadedBy = uploadedBy,
            Status = DocumentStatus.Pending
        };

        // Upload to Blob Storage
        var blobUrl = await _blobStorage.UploadAsync(
            AzureConstants.DocumentsContainer,
            document.FileName,
            fileStream,
            contentType);

        document.BlobUrl = blobUrl;

        // Save metadata to Cosmos DB
        await _cosmosDb.CreateDocumentAsync(document);

        // Queue for processing
        var message = new ProcessDocumentMessage(
            document.Id,
            document.FileName,
            AzureConstants.DocumentsContainer);

        await _queueService.SendMessageAsync(AzureConstants.ProcessingQueue, message);

        _logger.LogInformation("Document {Id} uploaded and queued for processing", document.Id);

        return new DocumentUploadResponse(
            document.Id,
            document.OriginalFileName,
            document.Status.ToString(),
            document.CreatedAt);
    }

    public async Task<DocumentResponse?> GetDocumentAsync(string id)
    {
        var document = await _cosmosDb.GetDocumentAsync(id);
        return document is null ? null : MapToResponse(document);
    }

    public async Task<DocumentListResponse> GetDocumentsAsync(int page, int pageSize)
    {
        var documents = await _cosmosDb.GetDocumentsAsync(page, pageSize);
        var totalCount = await _cosmosDb.GetDocumentCountAsync();

        return new DocumentListResponse(
            documents.Select(MapToResponse),
            totalCount,
            page,
            pageSize);
    }

    public async Task<Stream> DownloadDocumentAsync(string id)
    {
        var document = await _cosmosDb.GetDocumentAsync(id)
            ?? throw new KeyNotFoundException($"Document {id} not found");

        return await _blobStorage.DownloadAsync(AzureConstants.DocumentsContainer, document.FileName);
    }

    public async Task<string> GetDownloadUrlAsync(string id, TimeSpan? expiresIn = null)
    {
        var document = await _cosmosDb.GetDocumentAsync(id)
            ?? throw new KeyNotFoundException($"Document {id} not found");

        return await _blobStorage.GenerateSasUrlAsync(
            AzureConstants.DocumentsContainer,
            document.FileName,
            expiresIn ?? TimeSpan.FromHours(1));
    }

    public async Task DeleteDocumentAsync(string id)
    {
        var document = await _cosmosDb.GetDocumentAsync(id)
            ?? throw new KeyNotFoundException($"Document {id} not found");

        await _blobStorage.DeleteAsync(AzureConstants.DocumentsContainer, document.FileName);

        if (document.ThumbnailUrl is not null)
        {
            var thumbnailName = Path.GetFileName(new Uri(document.ThumbnailUrl).LocalPath);
            await _blobStorage.DeleteAsync(AzureConstants.ThumbnailsContainer, thumbnailName);
        }

        await _cosmosDb.DeleteDocumentAsync(id);

        _logger.LogInformation("Document {Id} deleted", id);
    }

    private static DocumentResponse MapToResponse(Document document) => new(
        document.Id,
        document.OriginalFileName,
        document.ContentType,
        document.FileSize,
        document.Status.ToString(),
        document.ThumbnailUrl,
        document.Metadata is null ? null : new DocumentMetadataDto(
            document.Metadata.PageCount,
            document.Metadata.Author,
            document.Metadata.Title,
            document.Metadata.CustomProperties),
        document.UploadedBy,
        document.CreatedAt,
        document.ProcessedAt);
}
