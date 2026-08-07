using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.UserStream.Contracts;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Redis.Messaging.Contracts;
using UserStreamService.Configuration;

namespace UserStreamService.Services;

public sealed class HealingPublisher(
    IBinanceOrdersSnapshotProvider snapshots,
    IRedisMessagePublisher publisher,
    IOptions<UserStreamServiceOptions> options,
    TimeProvider time,
    ILogger<HealingPublisher> logger)
{
    private readonly UserStreamServiceOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset? _lastPublishedAtUtc;

    public async Task PublishAfterReconnectAsync(TimeSpan downtime, CancellationToken ct)
    {
        if (downtime.TotalSeconds < _options.MinDowntimeForHealingSeconds)
            return;

        await _gate.WaitAsync(ct);

        try
        {
            var now = time.GetUtcNow();

            if (_lastPublishedAtUtc is not null && now - _lastPublishedAtUtc < TimeSpan.FromSeconds(_options.HealingCooldownSeconds))
                return;

            var symbols = _options.HealingSymbols.Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var symbol in symbols)
            {
                ct.ThrowIfCancellationRequested();

                var snapshot = await snapshots.GetAsync(symbol, ct);
                var activeClientIds = Collect(snapshot.NormalOrders, snapshot.AlgoOrders);

                var message = new HealingSnapshotMessage
                {
                    HubTimestampUtc = now.UtcDateTime,
                    SnapshotTimestampUtc = now.UtcDateTime,
                    DowntimeSeconds = Math.Round(downtime.TotalSeconds, 1),
                    Symbol = symbol,
                    NormalOrdersCount = snapshot.NormalOrders.Length,
                    AlgoOrdersCount = snapshot.AlgoOrders.Length,
                    TotalOrdersCount = activeClientIds.Count,
                    ActiveClientIds = activeClientIds,
                    NormalOrders = snapshot.NormalOrders,
                    AlgoOrders = snapshot.AlgoOrders
                };

                await publisher.PublishAsync(RedisChannels.Healing, message, ct);

                logger.LogInformation("Healing snapshot published. Symbol = {Symbol}, Active = {Count}, DowntimeSeconds = {DowntimeSeconds}", symbol, activeClientIds.Count, message.DowntimeSeconds);
            }

            _lastPublishedAtUtc = now;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static IReadOnlyCollection<string> Collect(IEnumerable<JsonElement> normalOrders, IEnumerable<JsonElement> algoOrders)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var order in normalOrders)
            Add(order, "clientOrderId", result);

        foreach (var order in algoOrders)
            Add(order, "clientAlgoId", result);

        return result.ToArray();
    }

    private static void Add(JsonElement element, string propertyName, ISet<string> result)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return;

        var value = property.GetString();

        if (!string.IsNullOrWhiteSpace(value))
            result.Add(value);
    }
}
