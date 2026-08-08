namespace TradingSystem.Dashboard.Contracts.Models.Optimization;

public sealed record OptimizationTrialDto(Guid TrialId, int Rank, IReadOnlyDictionary<string, string> Parameters, decimal Score, decimal NetProfit, decimal DrawdownPercent, decimal WinRatePercent, int Trades, bool Selected);
