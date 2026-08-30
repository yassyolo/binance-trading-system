using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Redis.Messaging.Contracts;
using UserStreamService.Configuration;

namespace UserStreamService.Services;

public sealed class UserStreamEventProcessor(
    IRedisMessagePublisher publisher,
    TimeProvider time,
    IOptions<UserStreamServiceOptions> options,
    ILogger<UserStreamEventProcessor> logger)
{
    private long _sequence;
    private readonly UserStreamServiceOptions _options = options.Value;

    public async Task ProcessAsync(string raw, CancellationToken ct)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            if (!root.TryGetProperty("e", out var eventProperty) 
                || eventProperty.ValueKind != JsonValueKind.String 
                || string.IsNullOrWhiteSpace(eventProperty.GetString()))
            {
                logger.LogWarning("Binance user-stream message has no valid event type.");
                return;
            }

            var envelope = new UserStreamEnvelope
            {
                HubTimestampUtc = time.GetUtcNow().UtcDateTime,
                HubSequence = Interlocked.Increment(ref _sequence),
                Binance = root.Clone()
            };

            if (_options.PublishRaw)
                await publisher.PublishAsync(RedisChannels.UserStreamRaw, envelope, ct);

            var eventType = eventProperty.GetString()!;
            switch (eventType)
            {
                case "ORDER_TRADE_UPDATE":
                case "ALGO_UPDATE":
                case "TRADE_LITE":
                    await publisher.PublishAsync(RedisChannels.UserStreamOrder, envelope, ct);
                    break;

                case "ACCOUNT_UPDATE":
                    await publisher.PublishAsync(RedisChannels.UserStreamAccount, envelope, ct);
                    break;

                case "listenKeyExpired":
                    logger.LogWarning("Binance listen key expired event received.");
                    break;

                default:
                    logger.LogDebug("Ignored Binance user event {EventType}", eventType);
                    break;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid Binance user-stream JSON.");
        }
    }
}
