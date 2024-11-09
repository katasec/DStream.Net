using Microsoft.Extensions.Logging;

namespace DStream.Net.ChangePublishers;

public class EventHubPublisher : IChangePublisher
{
    private readonly string _connectionString;
    private readonly ILogger<EventHubPublisher> _logger;

    public EventHubPublisher(string connectionString, ILogger<EventHubPublisher> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task SendAsync(MonitoringMessage message)
    {
        _logger.LogInformation($"Sending message to Event Hub: {message.Message}");
        // Code to send the message to Azure Event Hub goes here
        await Task.CompletedTask;
    }
}
