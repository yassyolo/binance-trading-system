using TradingSystem.Domain.Enums;

namespace TradingSystem.Application.Execution;

public interface IBotTradeExecutor
{
    string BotName {  get;  }
    Task<TradeExecutionResult> OpenAsync(string symbol,  PositionSide side,  string? source,  CancellationToken ct);
    Task<TradeExecutionResult> CloseAsync(string shortId,  string reason,  CancellationToken ct);
}
