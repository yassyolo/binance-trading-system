using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using TradingSystem.Application.Events;
using TradingSystem.Application.Orders;
using TradingSystem.Binance.Execution;
using TradingSystem.Contracts.Messaging;
using TradingSystem.HistoricalDatabase;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History;
using TradingSystem.Prometheus;

namespace StrategyService.Subscribers;

public sealed class UserStreamOrderSubscriber(
    IConnectionMultiplexer redis,
    IEnumerable<IBotOrderEventHandler> handlers,
    IEventDeduplicationStore dedup,
    ITradingPipelineRecorder history,
    IHistoricalEventSink historicalEvents,
    TradingMetrics metrics,
    ITradingEnvironmentProvider environment,
    ILogger<UserStreamOrderSubscriber> logger) : BackgroundService
{
    private readonly IReadOnlyDictionary<string, IBotOrderEventHandler> _handlers =
        handlers.ToDictionary(x => x.BotName, StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var subscriber = redis.GetSubscriber();
        var channel = RedisChannel.Literal(RedisChannels.UserStreamOrder);

        await subscriber.SubscribeAsync(channel, async (_, message) =>
        {
            if (message.HasValue)
                await ProcessAsync(message!, cancellationToken);
        });

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            await subscriber.UnsubscribeAsync(channel);
        }
    }

    private async Task ProcessAsync(string raw, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            var binance = document.RootElement.TryGetProperty("binance", out var wrapper)
                ? wrapper
                : document.RootElement;

            var eventType = GetString(binance, "e");
            if (eventType is not ("ORDER_TRADE_UPDATE" or "ALGO_UPDATE"))
                return;

            var order = binance.TryGetProperty("o", out var node)
                ? node
                : binance.TryGetProperty("ao", out node)
                    ? node
                    : binance;

            var clientId = GetString(order, "c")
                           ?? GetString(order, "clientOrderId")
                           ?? GetString(order, "clientAlgoId")
                           ?? GetString(order, "caid");

            if (!BinanceClientOrderId.TryParse(clientId, out var bot, out var role, out var shortId) ||
                !_handlers.TryGetValue(bot, out var handler))
            {
                return;
            }

            var status = GetString(order, "X")
                         ?? GetString(order, "orderStatus")
                         ?? GetString(order, "algoStatus")
                         ?? GetString(order, "status");
            var orderId = GetString(order, "i")
                          ?? GetString(order, "orderId")
                          ?? GetString(order, "algoId");
            var symbol = GetString(order, "s") ?? "unknown";
            var key = $"{eventType}:{orderId}:{clientId}:{status}";

            if (!await dedup.TryBeginAsync(key, TimeSpan.FromHours(24), cancellationToken))
                return;

            var quantity = GetDecimal(order, "q");
            var executed = GetDecimal(order, "z");
            var price = GetDecimal(order, "p");

            metrics.OrderEvents
                .WithLabels(bot, symbol, role, status ?? "unknown")
                .Inc();

            await history.RecordOrderEventAsync(new OrderEventHistoryRecord(
                key,
                bot,
                shortId,
                clientId,
                orderId,
                role,
                status,
                GetString(order, "S"),
                symbol,
                environment.EnvironmentName,
                DateTime.UtcNow,
                price,
                quantity,
                executed,
                raw), cancellationToken);

            await historicalEvents.WriteAsync(new HistoricalEvent(
                Guid.NewGuid(),
                HistoricalEventType.OrderUpdate,
                DateTime.UtcNow,
                environment.EnvironmentName,
                key,
                bot,
                null,
                symbol,
                shortId,
                orderId,
                GetString(order, "S"),
                status,
                price,
                quantity,
                null,
                null,
                new Dictionary<string, object?>
                {
                    ["event_type"] = eventType,
                    ["role"] = role,
                    ["client_id"] = clientId,
                    ["executed_quantity"] = executed
                },
                raw), cancellationToken);

            if (role == "TP" && status?.Equals("FILLED", StringComparison.OrdinalIgnoreCase) == true)
                await handler.HandleTpFilledAsync(shortId, executed, cancellationToken);
            else if (role == "TP" && status is "CANCELED" or "EXPIRED" or "REJECTED")
                await handler.HandleTpTerminalAsync(shortId, status, cancellationToken);
            else if (role == "SL" && IsTriggered(status))
                await handler.HandleSlTriggeredAsync(shortId, cancellationToken);
            else if (role is "S3" or "STOP3" && IsTriggered(status))
                await handler.HandleStop3TriggeredAsync(shortId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            metrics.ProcessingFailures
                .WithLabels("user_stream_order", "unknown", exception.GetType().Name)
                .Inc();
            logger.LogError(exception, "Order event processing failed.");
        }
    }

    private static bool IsTriggered(string? status) =>
        status is "FILLED" or "FINISHED" or "TRIGGERED";

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
            ? value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : value.GetRawText()
            : null;

    private static decimal GetDecimal(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) &&
        decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0m;
}
