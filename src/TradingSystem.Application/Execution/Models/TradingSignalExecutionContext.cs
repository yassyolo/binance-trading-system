namespace TradingSystem.Application.Execution.Models;

public sealed record TradingSignalExecutionContext(
    string SignalId,
    string StrategyVersion,
    string? Source);

