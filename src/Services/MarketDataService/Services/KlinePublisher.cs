using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Redis;

namespace MarketDataService.Services;

public sealed class KlinePublisher(
    IConnectionMultiplexer redis,
    ILogger<KlinePublisher> logger)
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task PublishClosedKlineAsync(JsonElement kline)
    {
        var symbol = kline.GetProperty("s").GetString()?.ToUpperInvariant();
        var interval = kline.GetProperty("i").GetString()?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(interval))
        {
            logger.LogWarning("Invalid kline payload: {Payload}", kline.ToString());
            return;
        }

        var key = RedisKeys.Kline(symbol, interval);
        var channel = RedisChannels.Kline(interval, symbol);

        var rawBinancePayload = kline.GetRawText();

        var cleanPayload = new ClosedKlineMessage(
            Symbol: symbol,
            Time: kline.GetProperty("t").GetInt64(),
            Open: kline.GetProperty("o").GetString(),
            High: kline.GetProperty("h").GetString(),
            Low: kline.GetProperty("l").GetString(),
            Close: kline.GetProperty("c").GetString(),
            Volume: kline.GetProperty("v").GetString(),
            CloseTime: kline.GetProperty("T").GetInt64(),
            Interval: interval);

        var cleanJson = JsonSerializer.Serialize(cleanPayload);

        await _database.StringSetAsync(key, rawBinancePayload);
        await _database.PublishAsync(RedisChannel.Literal(channel), cleanJson);

        logger.LogInformation(
            "Closed kline published. Symbol={Symbol}, Interval={Interval}, Close={Close}, Key={Key}, Channel={Channel}",
            symbol,
            interval,
            cleanPayload.Close,
            key,
            channel);
    }
}