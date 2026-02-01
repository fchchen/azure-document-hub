using System.Text.Json;
using Azure.Storage.Blobs;
using DocumentHub.Shared.Constants;
using DocumentHub.Shared.DTOs;
using DocumentHub.Shared.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DocumentHub.Functions.Functions;

public class ProcessDocumentFunction
{
    private readonly Container _container;
    private readonly ILogger<ProcessDocumentFunction> _logger;

    public ProcessDocumentFunction(Container container, ILogger<ProcessDocumentFunction> logger)
    {
        _container = container;
        _logger = logger;
    }

    [Function(nameof(ProcessDocument))]
    public async Task ProcessDocument(
        [QueueTrigger(AzureConstants.ProcessingQueue, Connection = "AzureWebJobsStorage")] string messageJson)
    {
        var message = JsonSerializer.Deserialize<ProcessDocumentMessage>(messageJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (message is null)
        {
            _logger.LogError("Failed to deserialize queue message");
            return;
        }

        _logger.LogInformation("Processing document {DocumentId}", message.DocumentId);

        try
        {
            // Get document from Cosmos DB
            var response = await _container.ReadItemAsync<Document>(
                message.DocumentId,
                new PartitionKey(message.DocumentId));

            var document = response.Resource;

            // Update status to processing
            document.Status = DocumentStatus.Processing;
            await _container.UpsertItemAsync(document, new PartitionKey(document.Id));

            // Simulate document processing (extract metadata)
            // In production, you would use Azure Cognitive Services or similar
            var metadata = await ExtractMetadataAsync(document);

            // Update document with processed metadata
            document.Metadata = metadata;
            document.Status = DocumentStatus.Completed;
            document.ProcessedAt = DateTime.UtcNow;

            await _container.UpsertItemAsync(document, new PartitionKey(document.Id));

            _logger.LogInformation("Document {DocumentId} processed successfully", message.DocumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", message.DocumentId);

            // Update document status to failed
            try
            {
                var response = await _container.ReadItemAsync<Document>(
                    message.DocumentId,
                    new PartitionKey(message.DocumentId));

                var document = response.Resource;
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = ex.Message;

                await _container.UpsertItemAsync(document, new PartitionKey(document.Id));
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to update document status to failed");
            }

            throw;
        }
    }

    private Task<DocumentMetadata> ExtractMetadataAsync(Document document)
    {
        // Simulate metadata extraction based on file type
        var metadata = new DocumentMetadata
        {
            CustomProperties = new Dictionary<string, string>
            {
                ["processedBy"] = "Azure Functions",
                ["processingVersion"] = "1.0"
            }
        };

        // Add file type specific metadata
        if (document.ContentType.StartsWith("image/"))
        {
            metadata.CustomProperties["type"] = "image";
        }
        else if (document.ContentType == "application/pdf")
        {
            metadata.PageCount = 1; // Would use PDF library in production
            metadata.CustomProperties["type"] = "pdf";
        }
        else if (document.ContentType.Contains("word"))
        {
            metadata.CustomProperties["type"] = "document";
        }

        return Task.FromResult(metadata);
    }
}
