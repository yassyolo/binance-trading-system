using System.Text.Json;
using Microsoft.Extensions.Options;
using UserStreamService.Clients;
using UserStreamService.Configuration;
using UserStreamService.Models;

namespace UserStreamService.Services;

public sealed class HealingService(
    BinanceOrdersSnapshotClient snapshotClient,
    RedisPublisher publisher,
    IOptions<UserStreamOptions> options,
    TimeProvider timeProvider,
    ILogger<HealingService> logger)
{
    private readonly UserStreamOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset? _lastPublishedAt;

    public async Task PublishAfterReconnectAsync(
        TimeSpan downtime,
        CancellationToken cancellationToken)
    {
        if (downtime.TotalSeconds < _options.MinDowntimeForHealingSeconds)
        {
            logger.LogInformation(
                "Healing skipped. DowntimeSeconds={DowntimeSeconds:F1}, ThresholdSeconds={ThresholdSeconds}",
                downtime.TotalSeconds,
                _options.MinDowntimeForHealingSeconds);
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (_lastPublishedAt is not null &&
                now - _lastPublishedAt.Value < TimeSpan.FromSeconds(_options.HealingCooldownSeconds))
            {
                logger.LogDebug("Healing skipped because cooldown is active.");
                return;
            }

            var symbol = _options.HealingSymbol.Trim().ToUpperInvariant();
            var normalOrdersTask = snapshotClient.GetOpenNormalOrdersAsync(symbol, cancellationToken);
            var algoOrdersTask = snapshotClient.GetOpenAlgoOrdersAsync(symbol, cancellationToken);
            await Task.WhenAll(normalOrdersTask, algoOrdersTask);

            var normalOrders = await normalOrdersTask;
            var algoOrders = await algoOrdersTask;
            var clientIds = CollectClientIds(normalOrders, algoOrders);

            var snapshot = new HealingSnapshot
            {
                HubTimestamp = timeProvider.GetLocalNow().ToString("HH:mm:ss"),
                SnapshotTimestamp = now.ToString("O"),
                DowntimeSeconds = Math.Round(downtime.TotalSeconds, 1),
                Symbol = symbol,
                NormalOrdersCount = normalOrders.Length,
                AlgoOrdersCount = algoOrders.Length,
                TotalOrdersCount = clientIds.Count,
                ActiveClientIds = clientIds,
                NormalOrders = normalOrders,
                AlgoOrders = algoOrders
            };

            await publisher.PublishHealingAsync(snapshot);
            _lastPublishedAt = now;

            logger.LogInformation(
                "Healing snapshot published. Normal={Normal}, Algo={Algo}, ActiveClientIds={Count}",
                normalOrders.Length,
                algoOrders.Length,
                clientIds.Count);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static IReadOnlyCollection<string> CollectClientIds(
        IEnumerable<JsonElement> normalOrders,
        IEnumerable<JsonElement> algoOrders)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var order in normalOrders)
            Add(order, "clientOrderId", result);

        foreach (var order in algoOrders)
            Add(order, "clientAlgoId", result);

        return result.ToArray();
    }

    private static void Add(JsonElement element, string propertyName, ISet<string> target)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            var value = property.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                target.Add(value);
        }
    }
}
