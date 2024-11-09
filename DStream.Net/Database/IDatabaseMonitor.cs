using DStream.Net.Config;

namespace DStream.Net.Database;

public interface IDatabaseMonitor
{
    Task InitializeAsync();  // Initialize resources, connections, or checkpoints
    Task MonitorTableAsync(TableConfig tableConfig, CancellationToken cancellationToken);  // Monitor a specific table for changes
}
