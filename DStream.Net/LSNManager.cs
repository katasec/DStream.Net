using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DStream.Net;

public class LSNManager
{
    private readonly SqlConnection _dbConn;
    private readonly string _tableName;
    private readonly ILogger<LSNManager> _logger;
    private byte[] _currentLSN;

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
        _currentLSN = await LoadLastLSNAsync() ?? new byte[10]; // Default LSN if not found
        _logger.LogInformation($"Initialized LSN for {_tableName} with: {BitConverter.ToString(_currentLSN)}");
    }

    // Retrieves the current LSN value
    public byte[] GetCurrentLSN()
    {
        return _currentLSN;
    }

    // Updates the in-memory LSN to the latest processed LSN
    public void UpdateCurrentLSN(byte[] newLSN)
    {
        _currentLSN = newLSN;
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

        _currentLSN = newLSN;
        _logger.LogInformation($"Saved new LSN for {_tableName}: {BitConverter.ToString(newLSN)}");
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
}
