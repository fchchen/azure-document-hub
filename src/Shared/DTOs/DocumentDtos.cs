namespace DocumentHub.Shared.DTOs;

public record DocumentUploadResponse(
    string Id,
    string FileName,
    string Status,
    DateTime CreatedAt
);

public record DocumentResponse(
    string Id,
    string FileName,
    string ContentType,
    long FileSize,
    string Status,
    string? ThumbnailUrl,
    DocumentMetadataDto? Metadata,
    string UploadedBy,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);

public record DocumentMetadataDto(
    int? PageCount,
    string? Author,
    string? Title,
    Dictionary<string, string>? CustomProperties
);

public record DocumentListResponse(
    IEnumerable<DocumentResponse> Documents,
    int TotalCount,
    int Page,
    int PageSize
);

public record ProcessDocumentMessage(
    string DocumentId,
    string BlobName,
    string ContainerName
);
