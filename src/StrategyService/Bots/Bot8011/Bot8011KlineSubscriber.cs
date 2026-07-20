using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Contracts.Messaging;

namespace StrategyService.Bots.Bot8011;

public sealed class Bot8011KlineSubscriber(IConnectionMultiplexer redis, IOptions<Bot8011Options> options, Bot8011TrailingPriceCache cache) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var o = options.Value;
        var channel = RedisChannels.Kline(o.TrailingInterval, o.Symbol);
        var sub = redis.GetSubscriber();

        await sub.SubscribeAsync(
            RedisChannel.Literal(channel),
            async (_, m) =>
            {
                try
                {
                    using var d = JsonDocument.Parse(m.ToString());
                    var r = d.RootElement;
                    if ((r.TryGetProperty("close", out var v) || r.TryGetProperty("c", out v)) &&
                        decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var p) && p > 0)
                    {
                        cache.Set(p);
                    }
                }
                catch
                {
                }
                await Task.CompletedTask;
            });

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        finally
        {
            await sub.UnsubscribeAsync(RedisChannel.Literal(channel));
        }
    }
}
