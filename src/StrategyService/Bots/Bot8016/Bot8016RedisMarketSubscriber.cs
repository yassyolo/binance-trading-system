using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;




namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016RedisMarketSubscriber(
    IConnectionMultiplexer redis, 
    IOptions<Bot8016Options> options, 
    Bot8016MarketState state, 
    Bot8016EntryCoordinator entry, 
    Bot8016PositionLifecycleService lifecycle, 
    ILogger<Bot8016RedisMarketSubscriber> logger)
    : BackgroundService
{
    private readonly Bot8016Options _options  =  options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber  =  redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(_options.AlligatorChannel), 
            async (_,  message)  => 
            {
                if (!message.HasValue)
                    return;

                try
                {
                    var snapshot  =  ParseIndicator(message.ToString());
                    if (snapshot is not null)
                        state.UpdateIndicator(snapshot);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,  "BOT8016 indicator message failed.");
                }
                await Task.CompletedTask;
            });

        foreach (var interval in new[] { _options.EntryTimeframe,  _options.ExitTimeframe }
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var channel  =  $"futures_kline_channel:{interval}:{_options.Symbol}";

            await subscriber.SubscribeAsync(
                RedisChannel.Literal(channel), 
                async (_,  message)  => 
                {
                    if (!message.HasValue)
                        return;

                    try
                    {
                        var candle  =  ParseCandle(message.ToString());
                        if (candle is null  ||  !candle.IsClosed)
                            return;

                        state.UpdateCandle(candle);

                        if (candle.Interval.Equals(
                                _options.EntryTimeframe, 
                                StringComparison.OrdinalIgnoreCase))
                        {
                            var indicator  =  state.GetIndicator();
                            if (indicator is null)
                                return;

                            var age  =  DateTime.UtcNow - indicator.PublishedAtUtc;
                            if (age > TimeSpan.FromSeconds(_options.AlligatorMaxAgeSeconds))
                                return;

                            await entry.ProcessAsync(
                                candle, 
                                indicator, 
                                stoppingToken);
                        }

                        if (candle.Interval.Equals(
                                _options.ExitTimeframe, 
                                StringComparison.OrdinalIgnoreCase))
                        {
                            await lifecycle.TryCreateStop3AfterBreakoutAsync(
                                candle.Close, 
                                stoppingToken);

                            var indicator  =  state.GetIndicator();
                            if (indicator is not null)
                            {
                                await lifecycle.ExitOnTeethCrossAsync(
                                    candle.Close, 
                                    indicator.Teeth, 
                                    stoppingToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,  "BOT8016 kline message failed.");
                    }
                    await Task.CompletedTask;
                });

            logger.LogInformation("BOT8016 subscribed to {Channel}",  channel);
        }

        await Task.Delay(Timeout.Infinite,  stoppingToken);
    }

    private static Bot8016Candle? ParseCandle(string json)
    {
        using var doc  =  JsonDocument.Parse(json);
        var x  =  doc.RootElement;

        var symbol  =  GetString(x,  "symbol") ?? GetString(x,  "s") ?? "";
        var interval  =  GetString(x,  "interval") ?? GetString(x,  "i") ?? "";

        return new Bot8016Candle(
            symbol, 
            interval, 
            GetLong(x,  "open_time",  "t"), 
            GetLong(x,  "close_time",  "T"), 
            GetDecimal(x,  "open",  "o"), 
            GetDecimal(x,  "high",  "h"), 
            GetDecimal(x,  "low",  "l"), 
            GetDecimal(x,  "close",  "c"), 
            GetBool(x,  "is_closed",  "x",  defaultValue: true));
    }

    private static Bot8016IndicatorSnapshot? ParseIndicator(string json)
    {
        using var doc  =  JsonDocument.Parse(json);
        var root  =  doc.RootElement;

        if ((GetString(root, "type") ?? "") != "alligator_ma")
            return null;

        if (!root.TryGetProperty("indicators",  out var indicators))
            return null;

        return new Bot8016IndicatorSnapshot(
            GetString(root,  "symbol") ?? "", 
            GetString(root,  "timeframe") ?? "", 
            GetLong(root,  "candle_close_time"), 
            GetLong(root,  "published_at"), 
            GetNestedDecimal(indicators,  "alligator_jaw"), 
            GetNestedDecimal(indicators,  "alligator_teeth"), 
            GetNestedDecimal(indicators,  "alligator_lips"), 
            GetNestedDecimal(indicators,  "sma200"));
    }

    private static string? GetString(JsonElement e,  string name)
         =>  e.TryGetProperty(name,  out var v) ? v.GetString() : null;

    private static long GetLong(JsonElement e,  params string[] names)
    {
        foreach (var name in names)
        {
            if (!e.TryGetProperty(name,  out var v))
                continue;
            if (v.ValueKind == JsonValueKind.Number  &&  v.TryGetInt64(out var n))
                return n;
            if (long.TryParse(v.GetString(),  out n))
                return n;
        }
        return 0;
    }

    private static decimal GetDecimal(JsonElement e,  params string[] names)
    {
        foreach (var name in names)
        {
            if (!e.TryGetProperty(name,  out var v))
                continue;
            if (v.ValueKind == JsonValueKind.Number  &&  v.TryGetDecimal(out var n))
                return n;
            if (decimal.TryParse(v.GetString(),  NumberStyles.Any,  CultureInfo.InvariantCulture,  out n))
                return n;
        }
        return 0;
    }

    private static decimal GetNestedDecimal(JsonElement e,  string name)
    {
        if (!e.TryGetProperty(name,  out var node))
            return 0;
        return GetDecimal(node,  "value");
    }

    private static bool GetBool(JsonElement e,  string a,  string b,  bool defaultValue)
    {
        foreach (var name in new[] { a,  b })
        {
            if (!e.TryGetProperty(name,  out var v))
                continue;
            if (v.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return v.GetBoolean();
        }
        return defaultValue;
    }
}
