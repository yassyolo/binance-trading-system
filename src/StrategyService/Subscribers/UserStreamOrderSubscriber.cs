using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;
using TradingSystem.Application.Events;
using TradingSystem.Application.Orders;
using TradingSystem.Binance.Execution.Models;
using TradingSystem.BotRuntime.Configuration.Contracts;
using TradingSystem.Contracts.Messaging;
using TradingSystem.HistoricalDatabase.EventStore;
using TradingSystem.HistoricalDatabase.Models;
using TradingSystem.HistoricalDatabase.Models.Enums;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History.Models;
using TradingSystem.Observability.Pipeline;
using TradingSystem.Prometheus.PrometheusMetrics;

namespace StrategyService.Subscribers;

public sealed class UserStreamOrderSubscriber(
    IConnectionMultiplexer redis,
    IEnumerable<IBotOrderEventHandler> handlers,
    IEventDeduplicationStore deduplication,
    ITradingPipelineRecorder history,
    IHistoricalEventSink historicalEvents,
    TradingMetrics metrics,
    ITradingEnvironmentProvider environment,
    IBotRuntimeConfigurationProvider configurations,
    LivePositionLifecycleRecorder lifecycle,
    ILogger<UserStreamOrderSubscriber> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private readonly IReadOnlyDictionary<string, IBotOrderEventHandler> _handlers = handlers.ToDictionary(x => x.BotName, StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var channel = RedisChannel.Literal(RedisChannels.UserStreamOrder);

        while (!ct.IsCancellationRequested)
        {
            var subscriber = redis.GetSubscriber();
            var subscribed = false;

            try
            {
                await subscriber.SubscribeAsync(channel, async (_, message) =>
                {
                    if (!message.HasValue || ct.IsCancellationRequested)
                        return;

                    await ProcessAsync(message.ToString(), ct);
                });

                subscribed = true;
                
                logger.LogInformation("Subscribed to user-stream orders. Channel = {Channel}", channel);
               
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Could not subscribe to user-stream orders because Redis is unavailable. Channel = {Channel}. Retrying.", channel);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "User-stream order subscription failed. Channel = {Channel}. Retrying.", channel);
            }
            finally
            {
                if (subscribed)
                {
                    try
                    {
                        await subscriber.UnsubscribeAsync(channel);
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, "Could not unsubscribe cleanly from user-stream order channel {Channel}", channel);
                    }
                }
            }

            await DelayBeforeRetryAsync(ct);
        }

        logger.LogInformation("User-stream order subscriber stopped. Channel = {Channel}", channel);
    }

    private async Task ProcessAsync(string raw, CancellationToken ct)
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

            var order = binance.TryGetProperty("options", out var node)
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
            var quantity = GetDecimal(order, "q");
            var executed = GetDecimal(order, "z");
            var price = GetDecimal(order, "p");
           
            var key = $"{eventType}:{orderId}:{clientId}:{status}:{executed.ToString(CultureInfo.InvariantCulture)}";

            if (!await deduplication.TryBeginAsync(key, TimeSpan.FromHours(24), ct))
                return;

            metrics.OrderEvents.WithLabels(bot, symbol, role, status ?? "unknown").Inc();

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
                await ResolveEnvironmentAsync(bot, ct),
                DateTime.UtcNow,
                price,
                quantity,
                executed,
                raw), ct);

            await historicalEvents.WriteAsync(new HistoricalEvent(
                Guid.NewGuid(),
                HistoricalEventType.OrderUpdate,
                DateTime.UtcNow,
                await ResolveEnvironmentAsync(bot, ct),
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
                raw), ct);

            if (role == "TP" && status?.Equals("FILLED", StringComparison.OrdinalIgnoreCase) == true)
                await handler.HandleTpFilledAsync(shortId, executed, ct);
            else if (role == "TP" && status is "CANCELED" or "EXPIRED" or "REJECTED")
                await handler.HandleTpTerminalAsync(shortId, status, ct);
            else if (role == "SL" && IsTriggered(status))
                await handler.HandleSlTriggeredAsync(shortId, ct);
            else if (role is "S3" or "STOP3" && IsTriggered(status))
                await handler.HandleStop3TriggeredAsync(shortId, ct);

            await lifecycle.RecordClosedAsync(bot, shortId, status, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            metrics.ProcessingFailures.WithLabels("user_stream_order", "unknown", exception.GetType().Name).Inc();
            
            logger.LogError(exception, "Order event processing failed.");
        }
    }

    private async Task<string> ResolveEnvironmentAsync(string botName, CancellationToken ct)
    {
        var config = await configurations.GetAsync(botName, ct);

        return config is not null && !string.IsNullOrWhiteSpace(config.Environment)
            ? config.Environment.Trim()
            : environment.EnvironmentName;
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

    private static async Task DelayBeforeRetryAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(RetryDelay, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {}
    }
}
