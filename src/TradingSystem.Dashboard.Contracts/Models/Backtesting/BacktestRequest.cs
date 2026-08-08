namespace TradingSystem.Dashboard.Contracts.Models.Backtesting;

public sealed record BacktestRequest(string BotName,  string Symbol,  DateTime FromUtc,  DateTime ToUtc,  decimal InitialBalance,  string SignalSource,  decimal CommissionPercent,  decimal SlippagePercent,  IReadOnlyDictionary<string, string> Parameters);