using System.Text.Json;
using DStream.Net.Config;
using Microsoft.Extensions.Logging;
using System.Data;
using DStream.Net.Database.SQLServer;
using Microsoft.Data.SqlClient;

namespace DStream.Net.Database.SqlServer
{
    public delegate Task MonitoringCallback(MonitoringMessage message);

    public class SQLServerMonitor : IDatabaseMonitor
    {
        private readonly IDbConnection _dbConn;
        private readonly BackoffManager _backoffManager;
        private readonly LSNManager _lsnManager;
        private readonly CheckpointManager _checkpointManager;
        private readonly MonitoringCallback _callback;
        private readonly ILogger<SQLServerMonitor> _logger;
        private readonly string _tableName;

        public SQLServerMonitor(
            IDbConnection dbConn,
            string tableName,
            TimeSpan initialInterval,
            TimeSpan maxInterval,
            MonitoringCallback callback,
            ILogger<SQLServerMonitor> logger,
            ILoggerFactory loggerFactory)
        {
            _dbConn = dbConn;
            _backoffManager = new BackoffManager(initialInterval, maxInterval);

            // Create a logger for LSNManager using loggerFactory
            _lsnManager = new LSNManager((SqlConnection)dbConn, tableName, loggerFactory.CreateLogger<LSNManager>());

            // Use injected logger for CheckpointManager
            _checkpointManager = new CheckpointManager(dbConn, tableName, loggerFactory.CreateLogger<CheckpointManager>());
            _callback = callback;
            _logger = logger;
            _tableName = tableName;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation($"Initializing SQLServerMonitor for table {_tableName}");

            try
            {
                await _checkpointManager.InitializeCheckpointTableAsync();
                await _lsnManager.InitializeAsync();
                _logger.LogInformation("Initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SQLServerMonitor.");
            }
        }

        public async Task MonitorTableAsync(TableConfig tableConfig, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting monitoring for table {tableConfig.Name}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var columns = await DatabaseMetadataHelper.GetColumnNamesAsync((SqlConnection)_dbConn, "dbo", tableConfig.Name);
                    var changeResult = await FetchCDCChangesAsync(tableConfig.Name, columns);

                    if (changeResult.ChangesFound)
                    {
                        foreach (var changeData in changeResult.ChangeDataList)
                        {
                            string jsonData = JsonSerializer.Serialize(changeData, new JsonSerializerOptions { WriteIndented = true });
                            await _callback(new MonitoringMessage(tableConfig.Name, "ChangeDetected", jsonData));
                        }

                        await _checkpointManager.SaveLastLSNAsync(changeResult.NewLSN);
                        _lsnManager.UpdateCurrentLSN(changeResult.NewLSN);
                        _backoffManager.Reset();
                        _logger.LogInformation($"Changes detected and processed for table {tableConfig.Name}.");
                    }
                    else
                    {
                        _backoffManager.Increase();
                        _logger.LogInformation($"No changes detected for table {tableConfig.Name}. Next poll in {_backoffManager.GetCurrentInterval()}.");
                    }

                    // Respect the cancellation token while delaying for the next poll interval
                    await Task.Delay(_backoffManager.GetCurrentInterval(), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation($"Monitoring for table {tableConfig.Name} has been canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error monitoring table {tableConfig.Name}");
                }
            }

            _logger.LogInformation($"Stopped monitoring table {tableConfig.Name}.");
        }

        private async Task<CDCChangeResult> FetchCDCChangesAsync(string tableName, List<string> columns)
        {
            var changeDataList = new List<Dictionary<string, object>>();
            string columnList = "__$start_lsn, __$operation, " + string.Join(", ", columns);
            string query = $@"
                SELECT {columnList}
                FROM cdc.dbo_{tableName}_CT
                WHERE __$start_lsn > @lastLSN
                ORDER BY __$start_lsn";

            using var cmd = new SqlCommand(query, (SqlConnection)_dbConn);
            cmd.Parameters.AddWithValue("@lastLSN", _lsnManager.GetCurrentLSN());

            byte[] latestLSN = null;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                latestLSN = (byte[])reader["__$start_lsn"];
                int operation = reader.GetInt32(reader.GetOrdinal("__$operation"));

                var changeData = new Dictionary<string, object>
                {
                    { "OperationType", operation == 2 ? "Insert" : operation == 4 ? "Update" : "Delete" }
                };

                foreach (var column in columns)
                {
                    changeData[column] = reader[column];
                }

                changeDataList.Add(changeData);
            }

            _logger.LogInformation($"Fetched {changeDataList.Count} changes for table {tableName}.");

            return new CDCChangeResult(changeDataList.Count > 0, latestLSN ?? _lsnManager.GetCurrentLSN(), changeDataList);
        }
    }
}
