namespace DocumentHub.Api.Services;

public interface IQueueService
{
    Task SendMessageAsync<T>(string queueName, T message);
}
