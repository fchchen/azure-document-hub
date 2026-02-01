namespace DocumentHub.Shared.Models;

public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? ThumbnailUrl { get; set; }
    public DocumentMetadata? Metadata { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DocumentMetadata
{
    public int? PageCount { get; set; }
    public string? Author { get; set; }
    public string? Title { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

public enum DocumentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}
