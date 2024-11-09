using Microsoft.Extensions.Logging;

namespace DStream.Net.ChangePublishers;

public class ServiceBusPublisher : IChangePublisher
{
    private readonly string _connectionString;
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(string connectionString, ILogger<ServiceBusPublisher> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task SendAsync(MonitoringMessage message)
    {
        _logger.LogInformation($"Sending message to Service Bus: {message.Message}");
        // Code to send the message to Azure Service Bus goes here
        await Task.CompletedTask;
    }
}
