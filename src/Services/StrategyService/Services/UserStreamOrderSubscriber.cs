using System.Globalization;
using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;

namespace StrategyService.Services;

public sealed class UserStreamOrderSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly Bot8011PositionEventService _bot8011Events;
    private readonly ILogger<UserStreamOrderSubscriber> _logger;

    public UserStreamOrderSubscriber(
        IConnectionMultiplexer redis,
        Bot8011PositionEventService bot8011Events,
        ILogger<UserStreamOrderSubscriber> logger)
    {
        _redis = redis;
        _bot8011Events = bot8011Events;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(RedisChannels.UserStreamOrder),
            async (_, message) =>
            {
                try
                {
                    if (!message.HasValue)
                        return;

                    await ProcessOrderEventAsync(message.ToString(), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // normal shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process user stream order event.");
                }
            });

        _logger.LogInformation("Subscribed to {Channel}", RedisChannels.UserStreamOrder);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessOrderEventAsync(string json, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("binance", out var binance))
            return;

        var eventType = GetString(binance, "e");

        switch (eventType)
        {
            case "ORDER_TRADE_UPDATE":
                await HandleOrderTradeUpdateAsync(binance, cancellationToken);
                break;

            case "ALGO_UPDATE":
                await HandleAlgoUpdateAsync(binance, cancellationToken);
                break;
        }
    }

    private async Task HandleOrderTradeUpdateAsync(
        JsonElement binance,
        CancellationToken cancellationToken)
    {
        if (!binance.TryGetProperty("o", out var order))
            return;

        var clientOrderId = GetString(order, "c");
        var status = GetString(order, "X");
        var executedQuantity = GetDecimal(order, "z");

        if (!TryParseClientId(clientOrderId, out var type, out var shortId))
            return;

        if (type == "TP" && IsFilledStatus(status))
        {
            await _bot8011Events.HandleTpFilledAsync(
                shortId,
                executedQuantity,
                cancellationToken);
        }
    }

    private async Task HandleAlgoUpdateAsync(
        JsonElement binance,
        CancellationToken cancellationToken)
    {
        if (!binance.TryGetProperty("o", out var order))
            return;

        var clientOrderId = GetString(order, "c");
        var status = GetString(order, "X");

        if (!TryParseClientId(clientOrderId, out var type, out var shortId))
            return;

        if (type == "SL" && IsFinalFilledStatus(status))
        {
            await _bot8011Events.HandleSlTriggeredAsync(shortId, cancellationToken);
            return;
        }

        if (type == "S3" && IsFinalFilledStatus(status))
        {
            await _bot8011Events.HandleStop3TriggeredAsync(shortId, cancellationToken);
        }
    }

    private static bool TryParseClientId(
        string? clientOrderId,
        out string type,
        out string shortId)
    {
        type = string.Empty;
        shortId = string.Empty;

        if (string.IsNullOrWhiteSpace(clientOrderId))
            return false;

        var parts = clientOrderId.Split('_', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
            return false;

        type = parts[1].ToUpperInvariant();
        shortId = parts[2];

        return !string.IsNullOrWhiteSpace(type) &&
               !string.IsNullOrWhiteSpace(shortId);
    }

    private static bool IsFilledStatus(string? status)
        => string.Equals(status, "FILLED", StringComparison.OrdinalIgnoreCase);

    private static bool IsFinalFilledStatus(string? status)
        => status is not null &&
           (status.Equals("FILLED", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("FINISHED", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("TRIGGERED", StringComparison.OrdinalIgnoreCase));

    private static string? GetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static decimal GetDecimal(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return 0;

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(
                value.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return parsed;
        }

        return 0;
    }
}