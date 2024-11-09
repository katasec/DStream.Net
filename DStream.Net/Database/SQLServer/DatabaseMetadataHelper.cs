using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DStream.Net.Database.SQLServer;

public static class DatabaseMetadataHelper
{
    // Retrieves column names for a specified table and schema
    public static async Task<List<string>> GetColumnNamesAsync(SqlConnection dbConn, string schema, string tableName)
    {
        const string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName";
        using var cmd = new SqlCommand(query, dbConn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@tableName", tableName);

        var columns = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }
        return columns;
    }
}
