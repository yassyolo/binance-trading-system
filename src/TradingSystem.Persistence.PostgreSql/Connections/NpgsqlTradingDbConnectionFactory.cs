using System.Data.Common;
using Npgsql;

namespace TradingSystem.Persistence.PostgreSql.Connections;

public sealed class NpgsqlTradingDbConnectionFactory(
    string connectionString, 
    int commandTimeoutSeconds)
    :ITradingDbConnectionFactory
{
    public int CommandTimeoutSeconds => commandTimeoutSeconds;
    
    public async Task<DbConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(connectionString);
        
        await connection.OpenAsync(ct);
        
        return connection;
    }
}
