using TradingSystem.Observability.Models;
namespace TradingSystem.Observability.Abstractions;

public interface ITradingHistoryStore
{
    Task AppendAsync(TradingHistoryEvent item, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TradingHistoryEvent> QueryAsync(TradingHistoryQuery query, CancellationToken cancellationToken = default);
}

public sealed record TradingHistoryQuery
{
    public string? BotName { get; init; }
    public string? Symbol { get; init; }
    public TradingHistoryEventType? Type { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public int Take { get; init; } = 10_000;
}
