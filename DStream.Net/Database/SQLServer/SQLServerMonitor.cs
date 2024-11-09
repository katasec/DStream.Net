using System.Text.Json;
using DStream.Net.Config;
using Microsoft.Extensions.Logging;
using System.Data;
using DStream.Net.Database.SQLServer;
using Microsoft.Data.SqlClient;
using DStream.Net.Database;

namespace DStream.Net.Database.SqlServer
{
    public class SQLServerMonitor : IDatabaseMonitor
    {
        private readonly IDbConnection _dbConn;
        private readonly BackoffManager _backoffManager;
        private readonly LSNManager _lsnManager;
        private readonly CheckpointManager _checkpointManager;
        private readonly ILogger<SQLServerMonitor> _logger;
        private readonly string _tableName;

        public SQLServerMonitor(
            IDbConnection dbConn,
            string tableName,
            TimeSpan initialInterval,
            TimeSpan maxInterval,
            ILogger<SQLServerMonitor> logger,
            ILoggerFactory loggerFactory)
        {
            _dbConn = dbConn;
            _tableName = tableName;
            _backoffManager = new BackoffManager(initialInterval, maxInterval);
            _lsnManager = new LSNManager((SqlConnection)dbConn, tableName, loggerFactory.CreateLogger<LSNManager>());
            _checkpointManager = new CheckpointManager(dbConn, tableName, loggerFactory.CreateLogger<CheckpointManager>());
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation($"Initializing SQLServerMonitor for table {_tableName}");
            await _checkpointManager.InitializeCheckpointTableAsync();
            await _lsnManager.InitializeAsync();
        }

        public async Task MonitorTableAsync(TableConfig tableConfig, CancellationToken cancellationToken, MonitoringCallback callback)
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
                            JsonSerializerOptions options = new() { WriteIndented = true };
                            string jsonData = JsonSerializer.Serialize(changeData, options);
                            await callback(new MonitoringMessage(tableConfig.Name, "ChangeDetected", jsonData));
                        }

                        await _checkpointManager.SaveLastLSNAsync(changeResult.NewLSN);
                        _lsnManager.UpdateCurrentLSN(changeResult.NewLSN);
                        _backoffManager.Reset();
                    }
                    else
                    {
                        _backoffManager.Increase();
                    }

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

            return new CDCChangeResult(changeDataList.Count > 0, latestLSN ?? _lsnManager.GetCurrentLSN(), changeDataList);
        }
    }
}
