using StackExchange.Redis;
using StrategyService.Infrastructure.Events;
using System.Globalization;
using System.Text.Json;
using TradingSystem.Application.Orders;
using TradingSystem.Contracts.Redis;

namespace StrategyService.Services;

public sealed class UserStreamOrderSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly OrderEventDeduplicationService _deduplication;
    private readonly ILogger<UserStreamOrderSubscriber> _logger;

    private readonly IReadOnlyDictionary<string, IBotOrderEventHandler> _handlers;

    public UserStreamOrderSubscriber(
        IConnectionMultiplexer redis,
        IEnumerable<IBotOrderEventHandler> handlers,
        OrderEventDeduplicationService deduplication,
        ILogger<UserStreamOrderSubscriber> logger)
    {
        _redis = redis;
        _handlers = handlers.ToDictionary(x => x.BotName, StringComparer.OrdinalIgnoreCase);
        _deduplication = deduplication;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(RedisChannels.UserStreamOrder),
            async (_, message) =>
            {
                if (!message.HasValue)
                    return;

                try
                {
                    await ProcessOrderEventAsync(message.ToString(), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
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

        var binance = root.TryGetProperty("binance", out var wrapped)
            ? wrapped
            : root;

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

        var clientOrderId =
            GetString(order, "c") ??
            GetString(order, "clientOrderId");

        var status =
            GetString(order, "X") ??
            GetString(order, "orderStatus");

        if (!TryParseClientId(clientOrderId, out var botName, out var type, out var shortId))
            return;

        var orderId =
            GetString(order, "i") ??
            GetString(order, "orderId");

        var eventKey = $"ORDER:{orderId}:{clientOrderId}:{status}";

        if (IsFinalStatus(status) &&
            _deduplication.IsDuplicate(eventKey, TimeSpan.FromHours(1)))
        {
            return;
        }

        var executedQuantity = GetDecimal(order, "z");

        if (!_handlers.TryGetValue(botName, out var handler))
            return;

        if (type == "TP" && IsFilledStatus(status))
        {
            await handler.HandleTpFilledAsync(shortId, executedQuantity, cancellationToken);
            return;
        }

        if (type == "TP" && IsTerminalNonFilledStatus(status))
        {
            await handler.HandleTpTerminalAsync(shortId, status!, cancellationToken);
            return;
        }
    }

    private async Task HandleAlgoUpdateAsync(
        JsonElement binance,
        CancellationToken cancellationToken)
    {
        var order = ExtractAlgoPayload(binance);

        var clientOrderId =
            GetString(order, "clientAlgoId") ??
            GetString(order, "caid") ??
            GetString(order, "c") ??
            GetString(order, "clientOrderId");

        var status =
            GetString(order, "algoStatus") ??
            GetString(order, "X") ??
            GetString(order, "x") ??
            GetString(order, "status");

        if (!TryParseClientId(clientOrderId, out var botName, out var type, out var shortId))
            return;

        var algoId =
            GetString(order, "algoId") ??
            GetString(order, "aid");

        var eventKey = $"ALGO:{algoId}:{clientOrderId}:{status}";

        if (IsFinalFilledStatus(status) &&
            _deduplication.IsDuplicate(eventKey, TimeSpan.FromHours(1)))
        {
            return;
        }

        if (!_handlers.TryGetValue(botName, out var handler))
            return;

        if (type == "SL" && IsFinalFilledStatus(status))
        {
            await handler.HandleSlTriggeredAsync(shortId, cancellationToken);
            return;
        }

        if ((type == "S3" || type == "STOP3") && IsFinalFilledStatus(status))
        {
            await handler.HandleStop3TriggeredAsync(shortId, cancellationToken);
        }
    }

    private static JsonElement ExtractAlgoPayload(JsonElement binance)
    {
        if (binance.TryGetProperty("o", out var order))
            return order;

        if (binance.TryGetProperty("ao", out var algoOrder))
            return algoOrder;

        if (binance.TryGetProperty("a", out var a))
            return a;

        return binance;
    }

    private static bool TryParseClientId(
        string? clientOrderId,
        out string botName,
        out string type,
        out string shortId)
    {
        botName = string.Empty;
        type = string.Empty;
        shortId = string.Empty;

        if (string.IsNullOrWhiteSpace(clientOrderId))
            return false;

        var parts = clientOrderId.Split('_', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
            return false;

        botName = parts[0].ToUpperInvariant();
        type = parts[1].ToUpperInvariant();
        shortId = parts[2];

        return botName.Length > 0 && type.Length > 0 && shortId.Length > 0;
    }

    private static bool IsFilledStatus(string? status)
        => status is not null &&
           status.Equals("FILLED", StringComparison.OrdinalIgnoreCase);

    private static bool IsFinalFilledStatus(string? status)
        => status is not null &&
           (status.Equals("FILLED", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("FINISHED", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("TRIGGERED", StringComparison.OrdinalIgnoreCase));

    private static bool IsTerminalNonFilledStatus(string? status)
        => status is not null &&
           (status.Equals("CANCELED", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("EXPIRED", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("REJECTED", StringComparison.OrdinalIgnoreCase));

    private static bool IsFinalStatus(string? status)
        => IsFilledStatus(status) || IsTerminalNonFilledStatus(status);

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
            return number;

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(
                value.GetString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed))
            return parsed;

        return 0;
    }
}