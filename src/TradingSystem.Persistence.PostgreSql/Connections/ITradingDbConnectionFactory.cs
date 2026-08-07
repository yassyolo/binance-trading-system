using System.Data.Common;

namespace TradingSystem.Persistence.PostgreSql.Connections;

public interface ITradingDbConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken ct);
    
    int CommandTimeoutSeconds{ get; }
}
