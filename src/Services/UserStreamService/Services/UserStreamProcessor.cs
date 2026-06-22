using System.Text.Json;

namespace UserStreamService.Services;

public sealed class UserStreamProcessor
{
    private readonly RedisPublisher _publisher;
    private readonly ILogger<UserStreamProcessor> _logger;

    private long _sequence;

    public UserStreamProcessor(
        RedisPublisher publisher,
        ILogger<UserStreamProcessor> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task ProcessAsync(string rawMessage)
    {
        using var document = JsonDocument.Parse(rawMessage);
        var root = document.RootElement;

        if (root.TryGetProperty("id", out _))
        {
            _logger.LogInformation("User stream control message received: {Message}", rawMessage);
            return;
        }

        if (!root.TryGetProperty("e", out var eventTypeElement))
        {
            _logger.LogDebug("Ignored non-event user stream message.");
            return;
        }

        var eventType = eventTypeElement.GetString() ?? "unknown";

        var wrapped = new
        {
            hub_ts = DateTime.Now.ToString("HH:mm:ss"),
            hub_seq = Interlocked.Increment(ref _sequence),
            binance = root
        };

        await _publisher.PublishRawAsync(wrapped);

        switch (eventType)
        {
            case "ORDER_TRADE_UPDATE":
                await _publisher.PublishOrderAsync(wrapped);
                _logger.LogInformation("ORDER_TRADE_UPDATE published.");
                break;

            case "ALGO_UPDATE":
                await _publisher.PublishOrderAsync(wrapped);
                _logger.LogInformation("ALGO_UPDATE published.");
                break;

            case "ACCOUNT_UPDATE":
                await _publisher.PublishAccountAsync(wrapped);
                _logger.LogInformation("ACCOUNT_UPDATE published.");
                break;

            default:
                _logger.LogInformation("Unsupported user stream event received. Type={EventType}", eventType);
                break;
        }
    }
}