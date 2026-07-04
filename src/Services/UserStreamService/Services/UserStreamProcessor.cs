using System.Text.Json;
using UserStreamService.Models;

namespace UserStreamService.Services;

public sealed class UserStreamProcessor(
    RedisPublisher publisher,
    ILogger<UserStreamProcessor> logger)
{
    private long _sequence;

    public async Task ProcessAsync(string rawMessage)
    {
        try
        {
            using var document = JsonDocument.Parse(rawMessage);
            var root = document.RootElement;

            if (root.TryGetProperty("id", out _))
            {
                logger.LogInformation("User stream control message received: {Message}", rawMessage);
                return;
            }

            if (!root.TryGetProperty("e", out var eventTypeElement))
            {
                logger.LogDebug("Ignored non-event user stream message.");
                return;
            }

            var eventType = eventTypeElement.GetString() ?? "unknown";

            var envelope = new UserStreamEnvelope
            {
                HubTimestamp = DateTime.Now.ToString("HH:mm:ss"),
                HubSequence = Interlocked.Increment(ref _sequence),
                Binance = root.Clone()
            };

            await publisher.PublishRawAsync(envelope);

            switch (eventType)
            {
                case "ORDER_TRADE_UPDATE":
                    await publisher.PublishOrderAsync(envelope);
                    logger.LogInformation("ORDER_TRADE_UPDATE published.");
                    break;

                case "ALGO_UPDATE":
                    await publisher.PublishOrderAsync(envelope);
                    logger.LogInformation("ALGO_UPDATE published.");
                    break;

                case "ACCOUNT_UPDATE":
                    await publisher.PublishAccountAsync(envelope);
                    logger.LogInformation("ACCOUNT_UPDATE published.");
                    break;

                default:
                    logger.LogInformation("Unsupported user stream event received. Type={EventType}", eventType);
                    break;
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid user stream JSON message.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error processing user stream message.");
        }
    }
}