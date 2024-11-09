using Microsoft.Data.SqlClient;

namespace DStream.Net;

public class CheckpointManager
{
    private readonly SqlConnection _dbConn;
    private readonly string _tableName;

    public CheckpointManager(SqlConnection dbConn, string tableName)
    {
        _dbConn = dbConn;
        _tableName = tableName;
    }

    // Ensures the checkpoint table exists
    public async Task InitializeCheckpointTableAsync()
    {
        const string query = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'cdc_offsets')
                BEGIN
                    CREATE TABLE cdc_offsets (
                        table_name NVARCHAR(255) PRIMARY KEY,
                        last_lsn VARBINARY(10),
                        updated_at DATETIME DEFAULT GETDATE()
                    );
                END";

        using var cmd = new SqlCommand(query, _dbConn);
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("Initialized checkpoints table if it didn't already exist.");
    }

    // Loads the last known LSN for the specified table; returns null if no LSN is found
    public async Task<byte[]?> LoadLastLSNAsync()
    {
        const string query = "SELECT last_lsn FROM cdc_offsets WHERE table_name = @tableName";
        using var cmd = new SqlCommand(query, _dbConn);
        cmd.Parameters.AddWithValue("@tableName", _tableName);

        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? null : (byte[])result;
    }

    // Saves the provided LSN as the last known LSN for the specified table
    public async Task SaveLastLSNAsync(byte[] newLSN)
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

        
        Console.WriteLine($"Saved new LSN for {_tableName}: {LSNManager.FormatLSN(newLSN)}");
    }
}
