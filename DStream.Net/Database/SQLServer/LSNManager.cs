using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DStream.Net.Database.SQLServer;

public class LSNManager
{
    private readonly SqlConnection _dbConn;
    private readonly string _tableName;
    private readonly ILogger<LSNManager> _logger;
    private byte[] _currentLSN;
    private readonly object _lsnLock = new object();  // Lock object for thread safety

    public LSNManager(SqlConnection dbConn, string tableName, ILogger<LSNManager> logger)
    {
        _dbConn = dbConn;
        _tableName = tableName;
        _logger = logger;
        _currentLSN = new byte[10]; // Default starting LSN
    }

    // Initializes the LSN by loading the last known LSN or using a default
    public async Task InitializeAsync()
    {
        var lastLSN = await LoadLastLSNAsync();
        lock (_lsnLock)
        {
            _currentLSN = lastLSN ?? new byte[10]; // Default LSN if not found
        }
        _logger.LogInformation($"Initialized LSN for {_tableName} with: {FormatLSN(_currentLSN)}");
    }

    // Retrieves the current LSN value
    public byte[] GetCurrentLSN()
    {
        lock (_lsnLock)
        {
            return _currentLSN;
        }
    }

    // Updates the in-memory LSN to the latest processed LSN
    public void UpdateCurrentLSN(byte[] newLSN)
    {
        lock (_lsnLock)
        {
            _currentLSN = newLSN;
        }
    }

    // Updates and saves the LSN to the database
    public async Task SaveLSNAsync(byte[] newLSN)
    {
        const string upsertQuery = @"
                MERGE INTO cdc_offsets AS target
                USING (VALUES (@tableName, @lastLSN, GETDATE())) AS source (table_name, last_lsn, updated_at)
                ON target.table_name = source.table_name
                WHEN MATCHED THEN 
                    UPDATE SET last_lsn = source.last_lsn, updated_at = source.updated_at
                WHEN NOT MATCHED THEN
                    INSERT (table_name, last_lsn, updated_at) 
                    VALUES (source.table_name, source.last_lsn, source.updated_at);";

        using var cmd = new SqlCommand(upsertQuery, _dbConn);
        cmd.Parameters.AddWithValue("@tableName", _tableName);
        cmd.Parameters.AddWithValue("@lastLSN", newLSN);
        await cmd.ExecuteNonQueryAsync();

        lock (_lsnLock)
        {
            _currentLSN = newLSN;
        }
        _logger.LogInformation($"Saved new LSN for {_tableName}: {FormatLSN(newLSN)}");
    }

    // Private helper to load the last LSN from the database
    private async Task<byte[]?> LoadLastLSNAsync()
    {
        const string query = "SELECT last_lsn FROM cdc_offsets WHERE table_name = @tableName";
        using var cmd = new SqlCommand(query, _dbConn);
        cmd.Parameters.AddWithValue("@tableName", _tableName);

        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? null : (byte[])result;
    }

    /// <summary>
    /// Formats the LSN byte array as a hexadecimal string prefixed with "0x".
    /// </summary>
    public static string FormatLSN(byte[] lsn)
    {
        return "0x" + BitConverter.ToString(lsn).Replace("-", string.Empty);
    }
}
