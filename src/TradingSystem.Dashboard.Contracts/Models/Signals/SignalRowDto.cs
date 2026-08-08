namespace TradingSystem.Dashboard.Contracts.Models.Signals;

public sealed record SignalRowDto(long Id, string SignalId, DateTime TimeUtc, string BotName, string Symbol, string Source, string Side, decimal? Price, string? Decision, string? BlockReason, string StrategyVersion, string Environment);

