namespace TradingSystem.Persistence.PostgreSql.Configuration;
public sealed class PostgresTradingHistoryOptions
{public const string SectionName = "TradingHistory";public string ConnectionStringName{ get; set;} = "TradingDatabase";public int CommandTimeoutSeconds{ get; set;} = 30;}
