using Microsoft.Extensions.Logging;

namespace DStream.Net.ChangePublishers;

public class ConsolePublisher : IChangePublisher
{
    private readonly ILogger<ConsolePublisher> _logger;

    public ConsolePublisher(ILogger<ConsolePublisher> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(MonitoringMessage message)
    {
        _logger.LogInformation($"ConsolePublisher: [{message.EventType}] {message.TableName}: {message.Message}");
        return Task.CompletedTask;
    }
}
