using System.Runtime.CompilerServices;
using System.Text.Json;
using TradingSystem.Observability.Abstractions;
using TradingSystem.Observability.Models;

namespace TradingSystem.Observability.Storage;

public sealed class JsonLinesTradingHistoryStore : ITradingHistoryStore
{
    private readonly string _directory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JsonLinesTradingHistoryStore(string directory) => _directory = directory;

    public async Task AppendAsync(TradingHistoryEvent item, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, $"trading-history-{item.OccurredAtUtc:yyyy-MM}.jsonl");
        var line = JsonSerializer.Serialize(item, JsonOptions) + Environment.NewLine;
        await _gate.WaitAsync(cancellationToken);
        try { await File.AppendAllTextAsync(path, line, cancellationToken); }
        finally { _gate.Release(); }
    }

    public async IAsyncEnumerable<TradingHistoryEvent> QueryAsync(TradingHistoryQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_directory)) yield break;
        var yielded = 0;
        foreach (var file in Directory.EnumerateFiles(_directory, "trading-history-*.jsonl").OrderBy(x => x))
        {
            await foreach (var line in File.ReadLinesAsync(file, cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                TradingHistoryEvent? item;
                try { item = JsonSerializer.Deserialize<TradingHistoryEvent>(line, JsonOptions); }
                catch (JsonException) { continue; }
                if (item is null || !Matches(item, query)) continue;
                yield return item;
                if (++yielded >= query.Take) yield break;
            }
        }
    }

    private static bool Matches(TradingHistoryEvent x, TradingHistoryQuery q) =>
        (q.BotName is null || x.BotName.Equals(q.BotName, StringComparison.OrdinalIgnoreCase)) &&
        (q.Symbol is null || x.Symbol.Equals(q.Symbol, StringComparison.OrdinalIgnoreCase)) &&
        (q.Type is null || x.Type == q.Type) &&
        (q.FromUtc is null || x.OccurredAtUtc >= q.FromUtc) &&
        (q.ToUtc is null || x.OccurredAtUtc <= q.ToUtc);
}
