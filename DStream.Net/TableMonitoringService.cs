using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using DStream.Net;
using Microsoft.Extensions.Logging;

namespace DStream.Net;

public class TableMonitoringService : BackgroundService, IAsyncDisposable
{
    private readonly AppConfig _appConfig;
    private readonly ILogger<TableMonitoringService> _logger;
    private readonly Channel<MonitoringMessage> _channel;
    private readonly List<Task> _monitoringTasks;
    private readonly List<SqlConnection> _dbConnections; // Track all connections
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed;

    public TableMonitoringService(AppConfig appConfig, ILogger<TableMonitoringService> logger, ILoggerFactory loggerFactory)
    {
        _appConfig = appConfig;
        _logger = logger;
        _channel = Channel.CreateUnbounded<MonitoringMessage>();
        _monitoringTasks = new List<Task>();
        _dbConnections = new List<SqlConnection>();
        _loggerFactory = loggerFactory;
        _disposed = false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_appConfig == null || string.IsNullOrEmpty(_appConfig.DbConnectionString) || _appConfig.Tables == null || _appConfig.Tables.Count == 0)
        {
            _logger.LogError("Configuration is missing essential values or no tables specified. Please check the config file.");
            return;
        }

        _logger.LogInformation($"Starting to monitor {_appConfig.Tables.Count} tables from config.");

        MonitoringCallback callback = async (message) =>
        {
            if (!_channel.Writer.TryWrite(message))
            {
                _logger.LogWarning("Failed to write message to channel.");
            }
        };

        foreach (var tableConfig in _appConfig.Tables)
        {
            _logger.LogInformation($"Starting to monitor table: {tableConfig.Name}");

            var dbConn = await CreateDbConnection(_appConfig.DbConnectionString);
            if (dbConn == null)
            {
                _logger.LogWarning($"Failed to connect to the database for table: {tableConfig.Name}");
                continue;
            }

            _dbConnections.Add(dbConn); // Track this connection

            var monitorTask = Task.Run(async () =>
            {
                using (dbConn) // Ensures connection disposal
                {
                    try
                    {
                        var sqlMonitorLogger = _loggerFactory.CreateLogger<SQLServerMonitor>();
                        var sqlMonitor = new SQLServerMonitor(
                            dbConn,
                            tableConfig.Name,
                            tableConfig.GetPollInterval(),
                            tableConfig.GetMaxPollInterval(),
                            callback,
                            sqlMonitorLogger,
                            _loggerFactory);

                        await sqlMonitor.InitializeAsync();
                        await sqlMonitor.MonitorTableAsync(tableConfig, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, $"Error monitoring table {tableConfig.Name}");
                    }
                }
            }, stoppingToken);

            _monitoringTasks.Add(monitorTask);
        }

        var readerTask = Task.Run(async () =>
        {
            await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                _logger.LogInformation($"[{message.EventType}] {message.TableName}: {message.Message}");
            }
        }, stoppingToken);

        _monitoringTasks.Add(readerTask);

        try
        {
            await Task.WhenAll(_monitoringTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Shutting down...");
        }
        finally
        {
            _channel.Writer.TryComplete();
        }
    }

    private async Task<SqlConnection?> CreateDbConnection(string connectionString)
    {
        try
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error opening database connection: {ex.Message}");
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing TableMonitoringService...");

            // Complete the channel to prevent new messages
            _channel.Writer.TryComplete();

            // Ensure all monitoring tasks are completed
            try
            {
                await Task.WhenAll(_monitoringTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown; no need to log
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while disposing monitoring tasks.");
            }

            // Close all active database connections
            foreach (var dbConn in _dbConnections)
            {
                if (dbConn.State != System.Data.ConnectionState.Closed)
                {
                    dbConn.Close();
                    _logger.LogInformation("Closed an open database connection during disposal.");
                }
                dbConn.Dispose();
            }

            _disposed = true;
            _logger.LogInformation("TableMonitoringService disposed.");
        }
    }
}
