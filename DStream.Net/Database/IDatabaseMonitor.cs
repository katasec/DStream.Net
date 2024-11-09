using DStream.Net.Config;
using System.Threading;
using System.Threading.Tasks;

namespace DStream.Net.Database;

public delegate Task MonitoringCallback(MonitoringMessage message);

public interface IDatabaseMonitor
{
    Task InitializeAsync();  // Initialize resources, connections, or checkpoints
    Task MonitorTableAsync(TableConfig tableConfig, CancellationToken cancellationToken, MonitoringCallback callback);  // Monitor a specific table for changes
}
