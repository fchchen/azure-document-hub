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
var allowInsecureCosmos = builder.Configuration.GetValue<bool>("CosmosDbAllowInsecure");

// Register Azure Blob Storage
builder.Services.AddSingleton(_ => new BlobServiceClient(storageConnectionString));

// Register Azure Queue Storage (use None encoding for Azure Functions compatibility)
builder.Services.AddSingleton(_ => new QueueServiceClient(storageConnectionString, new QueueClientOptions
{
    MessageEncoding = QueueMessageEncoding.None
}));

// Register Cosmos DB
if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    builder.Services.AddSingleton(sp =>
    {
        var options = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        if (allowInsecureCosmos)
        {
            options.HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return new HttpClient(handler);
            };
        }

        var client = new CosmosClient(cosmosConnectionString, options);
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

// Initialize Cosmos DB in background (non-blocking startup)
if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(AzureConstants.CosmosDatabase);
            await database.Database.CreateContainerIfNotExistsAsync(
                AzureConstants.DocumentsCollection,
                "/id");
            app.Logger.LogInformation("Cosmos DB initialized successfully");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to initialize Cosmos DB. Will retry on first request.");
        }
    });
}

// Always enable Swagger for demo purposes
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Map endpoints
app.MapDocumentEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
