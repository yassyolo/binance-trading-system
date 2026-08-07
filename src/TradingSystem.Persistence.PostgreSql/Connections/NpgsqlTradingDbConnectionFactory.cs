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
        var c = new NpgsqlConnection(connectionString);
        
        await c.OpenAsync(ct);return c;
    }
}
