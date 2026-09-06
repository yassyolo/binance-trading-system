using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.Signals;
using TradingSystem.Infrastructure.Serialization;

namespace TradingSystem.Redis.Signals;

public sealed class RedisTradingSignalSubscriber(
    IConnectionMultiplexer redis,
    ITradingSignalHandler signalHandler,
    IClock clock,
    ILogger<RedisTradingSignalSubscriber> logger)
    : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var channel = RedisChannel.Literal(RedisChannels.StrategySignals);

        while (!ct.IsCancellationRequested)
        {
            var subscriber = redis.GetSubscriber();
            var subscribed = false;

            try
            {
                await subscriber.SubscribeAsync(channel, async (_, value) =>
                {
                    if (!value.HasValue || ct.IsCancellationRequested)
                        return;

                    await ProcessSafelyAsync(value.ToString(), ct);
                });

                subscribed = true;
                
                logger.LogInformation("Subscribed to trading signals. Channel = {Channel}", channel);

                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (RedisException redisEx)
            {
                logger.LogWarning(redisEx, "Could not subscribe to trading signals because Redis is unavailable. Channel = {Channel}. Retrying.", channel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Trading signal subscription failed. Channel = {Channel}. Retrying.", channel);
            }
            finally
            {
                if (subscribed)
                {
                    try
                    {
                        await subscriber.UnsubscribeAsync(channel);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not unsubscribe cleanly from trading signal channel {Channel}", channel);
                    }
                }
            }

            await DelayBeforeRetryAsync(ct);
        }

        logger.LogInformation("Trading signal subscriber stopped. Channel = {Channel}", channel);
    }

    private async Task ProcessSafelyAsync(string raw, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Trading signal received from Redis. Raw = {Raw}", raw);

            var message = JsonSerializer.Deserialize<TradingSignalMessage>(raw, JsonDefaults.Messaging);
            if (message is null)
            {
                logger.LogWarning("Trading signal deserialization returned null. Raw = {Raw}", raw);
                return;
            }

            var signal = TradingSignalMessageMapper.Map(message, clock.UtcNow);
           
            await HandleWithTransientRetryAsync(signal, cancellationToken);

            logger.LogInformation("Trading signal signalHandler completed. BotName = {BotName}, Symbol = {Symbol}, Side = {Side}", signal.BotName, signal.Symbol, signal.Side);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {}
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "Trading signal JSON is invalid. Raw = {Raw}", raw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trading signal processing failed. Raw = {Raw}", raw);
        }
    }

    private async Task HandleWithTransientRetryAsync(Domain.Signals.TradeSignal signal, CancellationToken ct)
    {
        var deadline = clock.UtcNow.AddSeconds(60);
        var attempt = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
           
            attempt++;

            try
            {
                _ = await signalHandler.HandleAsync(signal, ct);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var remaining = deadline - clock.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    throw;

                var delay = TimeSpan.FromSeconds(Math.Min(attempt, 5));
                if (delay > remaining)
                    delay = remaining;

                logger.LogWarning(ex, "Trading signal hit a transient infrastructure failure. SignalId = {SignalId}, Attempt = {Attempt}. Retrying in {Delay}.", signal.SignalId, attempt, delay);

                await Task.Delay(delay, ct);
            }
        }
    }

    private static async Task DelayBeforeRetryAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(RetryDelay, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        { }
    }
}
