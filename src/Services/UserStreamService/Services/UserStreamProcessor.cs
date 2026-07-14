using System.Text.Json;
using UserStreamService.Models;

namespace UserStreamService.Services;

public sealed class UserStreamProcessor(
    RedisPublisher publisher,
    TimeProvider timeProvider,
    ILogger<UserStreamProcessor> logger)
{
    private long _sequence;

    public async Task ProcessAsync(string rawMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var document = JsonDocument.Parse(rawMessage);
            var root = document.RootElement;

            if (root.TryGetProperty("id", out _))
            {
                logger.LogInformation("User stream control response received. Message={Message}", rawMessage);
                return;
            }

            if (!root.TryGetProperty("e", out var eventTypeProperty))
                return;

            var eventType = eventTypeProperty.GetString() ?? "unknown";
            var envelope = new UserStreamEnvelope
            {
                HubTimestamp = timeProvider.GetLocalNow().ToString("HH:mm:ss"),
                HubSequence = Interlocked.Increment(ref _sequence),
                Binance = root.Clone()
            };

            await publisher.PublishRawAsync(envelope);

            switch (eventType)
            {
                case "ORDER_TRADE_UPDATE":
                case "ALGO_UPDATE":
                    await publisher.PublishOrderAsync(envelope);
                    break;

                case "ACCOUNT_UPDATE":
                    await publisher.PublishAccountAsync(envelope);
                    break;

                default:
                    logger.LogInformation("Unsupported Binance user event. EventType={EventType}", eventType);
                    break;
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid Binance user stream JSON.");
        }
    }
}
