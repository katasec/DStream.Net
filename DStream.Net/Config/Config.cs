namespace DStream.Net.Config;

public class AppConfig
{
    public string? DbType { get; set; }
    public string? DbConnectionString { get; set; }

    // Downstream provider configuration
    public string? DownstreamProviderType { get; set; } // e.g., "EventHub", "ServiceBus", "Console"
    public string? DownstreamConnectionString { get; set; } // Connection string for the selected downstream provider

    public List<TableConfig>? Tables { get; set; }
}
