using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DStream.Net.Config;
using DStream.Net.ChangePublishers;

namespace DStream.Net
{
    public static class ChangePublisherProviderFactory
    {
        public static IChangePublisher Create(AppConfig config, IServiceProvider services)
        {
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            // Check for required connection string if the provider type needs it
            if ((config.DownstreamProviderType == "EventHub" || config.DownstreamProviderType == "ServiceBus")
                && string.IsNullOrEmpty(config.DownstreamConnectionString))
            {
                throw new ArgumentNullException(nameof(config.DownstreamConnectionString), $"{config.DownstreamProviderType} connection string is required.");
            }

            return config.DownstreamProviderType switch
            {
                "EventHub" =>
                    new EventHubPublisher(config.DownstreamConnectionString!, loggerFactory.CreateLogger<EventHubPublisher>()),

                "ServiceBus" =>
                    new ServiceBusPublisher(config.DownstreamConnectionString!, loggerFactory.CreateLogger<ServiceBusPublisher>()),

                _ =>
                    new ConsolePublisher(loggerFactory.CreateLogger<ConsolePublisher>()) // Default to console if not specified
            };
        }
    }
}
