using System.Text.Json;
using Azure.Storage.Queues;

namespace DocumentHub.Api.Services;

public class QueueService : IQueueService
{
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ILogger<QueueService> _logger;

    public QueueService(QueueServiceClient queueServiceClient, ILogger<QueueService> logger)
    {
        _queueServiceClient = queueServiceClient;
        _logger = logger;
    }

    public async Task SendMessageAsync<T>(string queueName, T message)
    {
        var queueClient = _queueServiceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync();

        var json = JsonSerializer.Serialize(message);
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

        await queueClient.SendMessageAsync(base64);

        _logger.LogInformation("Sent message to queue {Queue}", queueName);
    }
}
