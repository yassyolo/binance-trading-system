using System.Text.Json;
using UserStreamService.Clients;
using UserStreamService.Models;

namespace UserStreamService.Services;

public sealed class HealingService(
    BinanceFuturesOrdersSnapshotClient snapshotClient,
    RedisPublisher publisher,
    IConfiguration configuration,
    ILogger<HealingService> logger)
{
    private DateTime? _lastHealingSentAtUtc;

    public async Task PublishHealingSnapshotAsync(double downtimeSeconds, CancellationToken cancellationToken)
    {
        var minDowntimeSeconds = configuration.GetValue<int>("UserStream:MinDowntimeForHealingSeconds", 30);

        if (downtimeSeconds < minDowntimeSeconds)
        {
            logger.LogInformation("Downtime {DowntimeSeconds}s is below healing threshold {Threshold}s. Skipping healing.", downtimeSeconds, minDowntimeSeconds);

            return;
        }

        var cooldownSeconds = configuration.GetValue<int>("UserStream:HealingCooldownSeconds", 60);

        if (_lastHealingSentAtUtc is not null &&
            DateTime.UtcNow - _lastHealingSentAtUtc.Value < TimeSpan.FromSeconds(cooldownSeconds))
        {
            logger.LogDebug("Healing snapshot skipped because cooldown is active.");
            return;
        }

        var symbol = configuration["UserStream:HealingSymbol"] ?? "BTCUSDC";

        logger.LogInformation("Publishing healing snapshot. Symbol={Symbol}, DowntimeSeconds={DowntimeSeconds}", symbol, downtimeSeconds);

        var normalOrders = await snapshotClient.GetOpenNormalOrdersAsync(symbol, cancellationToken);

        var algoOrders = await snapshotClient.GetOpenAlgoOrdersAsync(symbol, cancellationToken);

        var activeClientIds = new HashSet<string>();

        foreach (var order in normalOrders)
        {
            if (TryGetString(order, "clientOrderId", out var clientOrderId))
                activeClientIds.Add(clientOrderId);
        }

        foreach (var order in algoOrders)
        {
            if (TryGetString(order, "clientAlgoId", out var clientAlgoId))
                activeClientIds.Add(clientAlgoId);
        }

        var snapshot = new HealingSnapshot
        {
            HubTimestamp = DateTime.Now.ToString("HH:mm:ss"),
            SnapshotTimestamp = DateTime.UtcNow.ToString("O"),
            DowntimeSeconds = Math.Round(downtimeSeconds, 1),
            Symbol = symbol,
            NormalOrdersCount = normalOrders.Length,
            AlgoOrdersCount = algoOrders.Length,
            TotalOrdersCount = activeClientIds.Count,
            ActiveClientIds = activeClientIds.ToList(),
            NormalOrders = normalOrders,
            AlgoOrders = algoOrders
        };

        await publisher.PublishHealingAsync(snapshot);

        _lastHealingSentAtUtc = DateTime.UtcNow;

        logger.LogInformation("Healing snapshot published. NormalOrders={NormalOrders}, AlgoOrders={AlgoOrders}, ActiveClientIds={ActiveClientIds}",
            normalOrders.Length,
            algoOrders.Length,
            activeClientIds.Count);
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;

        if (!element.TryGetProperty(propertyName, out var property))
            return false;

        value = property.GetString() ?? string.Empty;

        return !string.IsNullOrWhiteSpace(value);
    }
}