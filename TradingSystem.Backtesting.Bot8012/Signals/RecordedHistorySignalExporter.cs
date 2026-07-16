using TradingSystem.Backtesting.Bot8012.Models;
using TradingSystem.Observability.Abstractions;
using TradingSystem.Observability.Models;
namespace TradingSystem.Backtesting.Bot8012.Signals;
public sealed class RecordedHistorySignalExporter(ITradingHistoryStore store)
{
    public async Task<IReadOnlyList<HistoricalSignal>> LoadAsync(string bot, string symbol, DateTime from, DateTime to, CancellationToken ct = default) { var r = new List<HistoricalSignal>(); await foreach (var e in store.QueryAsync(new TradingHistoryQuery { BotName = bot, Symbol = symbol, Type = TradingHistoryEventType.SignalReceived, FromUtc = from, ToUtc = to }, ct)) { if (Enum.TryParse<BacktestSide>(e.Side, true, out var side)) r.Add(new(e.OccurredAtUtc, side, e.Source ?? "recorded", e.SignalId)); } return r; }
}
