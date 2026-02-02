using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using DocumentHub.Api.Endpoints;
using DocumentHub.Api.Services;
using DocumentHub.Shared.Constants;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Document Hub API", Version = "v1" });
});

// Configure Azure Services
var storageConnectionString = builder.Configuration.GetConnectionString("AzureStorage")
    ?? "UseDevelopmentStorage=true";

var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDb");

// Register Azure Blob Storage
builder.Services.AddSingleton(_ => new BlobServiceClient(storageConnectionString));

// Register Azure Queue Storage
builder.Services.AddSingleton(_ => new QueueServiceClient(storageConnectionString));

// Register Cosmos DB
if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    builder.Services.AddSingleton(sp =>
    {
        var client = new CosmosClient(cosmosConnectionString, new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });
        return client;
    });
}

// Register Services
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<ICosmosDbService, CosmosDbService>();
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://purple-coast-01face30f.2.azurestaticapps.net")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Initialize Cosmos DB (async, after app starts)
if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    using var scope = app.Services.CreateScope();
    var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    try
    {
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(AzureConstants.CosmosDatabase);
        await database.Database.CreateContainerIfNotExistsAsync(
            AzureConstants.DocumentsCollection,
            "/id");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to initialize Cosmos DB. Will retry on first request.");
    }
}

// Always enable Swagger for demo purposes
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");
app.UseHttpsRedirection();

// Map endpoints
app.MapDocumentEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
