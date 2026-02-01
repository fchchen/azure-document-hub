using DocumentHub.Shared.Constants;
using DocumentHub.Shared.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace DocumentHub.Api.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbService> _logger;

    public CosmosDbService(CosmosClient cosmosClient, ILogger<CosmosDbService> logger)
    {
        _logger = logger;
        _container = cosmosClient.GetContainer(AzureConstants.CosmosDatabase, AzureConstants.DocumentsCollection);
    }

    public async Task<Document> CreateDocumentAsync(Document document)
    {
        var response = await _container.CreateItemAsync(document, new PartitionKey(document.Id));
        _logger.LogInformation("Created document {Id} in Cosmos DB", document.Id);
        return response.Resource;
    }

    public async Task<Document?> GetDocumentAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Document>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Document {Id} not found", id);
            return null;
        }
    }

    public async Task<IEnumerable<Document>> GetDocumentsAsync(int page, int pageSize)
    {
        var query = _container.GetItemLinqQueryable<Document>()
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToFeedIterator();

        var results = new List<Document>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<int> GetDocumentCountAsync()
    {
        var query = _container.GetItemLinqQueryable<Document>()
            .CountAsync();

        return await query;
    }

    public async Task<Document> UpdateDocumentAsync(Document document)
    {
        var response = await _container.UpsertItemAsync(document, new PartitionKey(document.Id));
        _logger.LogInformation("Updated document {Id} in Cosmos DB", document.Id);
        return response.Resource;
    }

    public async Task DeleteDocumentAsync(string id)
    {
        await _container.DeleteItemAsync<Document>(id, new PartitionKey(id));
        _logger.LogInformation("Deleted document {Id} from Cosmos DB", id);
    }

    public async Task<IEnumerable<Document>> GetDocumentsByStatusAsync(DocumentStatus status)
    {
        var query = _container.GetItemLinqQueryable<Document>()
            .Where(d => d.Status == status)
            .ToFeedIterator();

        var results = new List<Document>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }
}
