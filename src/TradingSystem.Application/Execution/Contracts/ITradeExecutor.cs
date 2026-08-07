using TradingSystem.Application.Execution.Models;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Execution.Contracts;

public interface ITradeExecutor
{
    Task<TradeExecutionResult> OpenAsync(string botName, string symbol, PositionSide side, string? source, CancellationToken ct);
    
    Task<TradeExecutionResult> CloseAsync(string botName, string shortId, string reason, CancellationToken ct);
}
