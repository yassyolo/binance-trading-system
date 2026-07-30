using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Time;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.Signals;
using TradingSystem.Infrastructure.Serialization;

namespace TradingSystem.Redis.Signals;

public sealed class RedisTradingSignalSubscriber(
    IConnectionMultiplexer redis,
    ITradingSignalHandler handler,
    IClock clock,
    ILogger<RedisTradingSignalSubscriber> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var subscriber = redis.GetSubscriber();
        var channel = RedisChannel.Literal(
            RedisChannels.StrategySignals);

        await subscriber.SubscribeAsync(
            channel,
            async (_, value) =>
            {
                if (!value.HasValue ||
                    stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                await ProcessSafelyAsync(
                    value.ToString(),
                    stoppingToken);
            });

        logger.LogInformation(
            "Subscribed to trading signals. Channel = {Channel}",
            channel);

        try
        {
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await subscriber.UnsubscribeAsync(channel);

            logger.LogInformation(
                "Unsubscribed from trading signals. Channel = {Channel}",
                channel);
        }
    }

    private async Task ProcessSafelyAsync(
        string raw,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Trading signal received from Redis. Raw = {Raw}",
                raw);

            var message =
                JsonSerializer.Deserialize<TradingSignalMessage>(
                    raw,
                    JsonDefaults.Messaging);

            if (message is null)
            {
                logger.LogWarning(
                    "Trading signal deserialization returned null. Raw = {Raw}",
                    raw);

                return;
            }

            logger.LogInformation(
                "Trading signal message deserialized. " +
                "BotName = {BotName}, Symbol = {Symbol}, " +
                "Side = {Side}, Source = {Source}, " +
                 "ReceivedAtUtc = {ReceivedAtUtc}",
                message.BotName,
                message.Symbol,
                message.Symbol,
                message.Source,
        
                message.GeneratedAtUtc);

            var signal = TradingSignalMessageMapper.Map(
                message,
                clock.UtcNow);

            logger.LogInformation(
                "Trading signal mapped. " +
                "BotName = {BotName}, Symbol = {Symbol}, " +
                "Side = {Side}, Source = {Source}",
                signal.BotName,
                signal.Symbol,
                signal.Side,
                signal.Source);

            logger.LogInformation(
                "Forwarding trading signal to handler. " +
                "BotName = {BotName}, Symbol = {Symbol}, Side = {Side}",
                signal.BotName,
                signal.Symbol,
                signal.Side);

            await handler.HandleAsync(
                signal,
                cancellationToken);

            logger.LogInformation(
                "Trading signal handler completed. " +
                "BotName = {BotName}, Symbol = {Symbol}, Side = {Side}",
                signal.BotName,
                signal.Symbol,
                signal.Side);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (JsonException exception)
        {
            logger.LogError(
                exception,
                "Trading signal JSON is invalid. Raw = {Raw}",
                raw);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Trading signal processing failed. Raw = {Raw}",
                raw);
        }
    }
}