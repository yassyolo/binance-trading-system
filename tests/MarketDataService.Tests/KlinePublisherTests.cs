using System.Reflection;
using System.Text.Json;
using MarketDataService.Configuration;
using MarketDataService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace MarketDataService.Tests;

public sealed class KlinePublisherTests
{
    [Fact]
    public async Task PublishAsync_ValidPayload_WritesLatestStateAndPublishesEvent()
    {
        var redis = RedisHarness.Create();
        var sut = CreateSut(redis, ttlSeconds: 120);

        await sut.PublishAsync(Kline("BTCUSDC", "1m", "60000", "60150", "59950", "60100", "23.4"), CancellationToken.None);

        Assert.Single(redis.StringSets);
        Assert.Single(redis.Publishes);
        Assert.Equal("kline:btcusdc:1m", redis.StringSets[0].Key);
        Assert.Equal(TimeSpan.FromSeconds(120), redis.StringSets[0].Expiry);
        Assert.Equal("futures_kline_channel:1m:BTCUSDC", redis.Publishes[0].Channel);
        Assert.Equal(redis.StringSets[0].Value, redis.Publishes[0].Value);

        using var document = JsonDocument.Parse(redis.StringSets[0].Value);
        var root = document.RootElement;
        Assert.Equal("BTCUSDC", root.GetProperty("symbol").GetString());
        Assert.Equal("1m", root.GetProperty("interval").GetString());
        Assert.Equal("60000", root.GetProperty("open").GetString());
        Assert.Equal("60100", root.GetProperty("close").GetString());
        Assert.Equal("23.4", root.GetProperty("volume").GetString());
    }

    [Fact]
    public async Task PublishAsync_NormalizesSymbolAndInterval()
    {
        var redis = RedisHarness.Create();
        var sut = CreateSut(redis);

        await sut.PublishAsync(Kline("  btcusdc  ", "  5M  ", "60000", "60150", "59950", "60100", "1"), CancellationToken.None);

        Assert.Equal("kline:btcusdc:5m", Assert.Single(redis.StringSets).Key);
        Assert.Equal("futures_kline_channel:5m:BTCUSDC", Assert.Single(redis.Publishes).Channel);

        using var document = JsonDocument.Parse(redis.StringSets[0].Value);
        Assert.Equal("BTCUSDC", document.RootElement.GetProperty("symbol").GetString());
        Assert.Equal("5m", document.RootElement.GetProperty("interval").GetString());
    }

    [Fact]
    public async Task PublishAsync_ExponentNotation_ParsesAndSerializesInvariantly()
    {
        var redis = RedisHarness.Create();
        var sut = CreateSut(redis);

        await sut.PublishAsync(Kline("BTCUSDC", "1m", "6e4", "6.015e4", "5.995e4", "6.01e4", "2.34e1"), CancellationToken.None);

        using var document = JsonDocument.Parse(Assert.Single(redis.StringSets).Value);
        var root = document.RootElement;
        Assert.Equal("60000", root.GetProperty("open").GetString());
        Assert.Equal("60150", root.GetProperty("high").GetString());
        Assert.Equal("59950", root.GetProperty("low").GetString());
        Assert.Equal("60100", root.GetProperty("close").GetString());
        Assert.Equal("23.4", root.GetProperty("volume").GetString());
    }

    [Theory]
    [MemberData(nameof(InvalidPayloads))]
    public async Task PublishAsync_InvalidPayload_DoesNotWriteOrPublish(JsonElement payload)
    {
        var redis = RedisHarness.Create();
        var sut = CreateSut(redis);

        await sut.PublishAsync(payload, CancellationToken.None);

        Assert.Empty(redis.StringSets);
        Assert.Empty(redis.Publishes);
    }

    [Fact]
    public async Task PublishAsync_StringSetFails_DoesNotPublishEvent()
    {
        var redis = RedisHarness.Create();
        redis.StringSetException = new InvalidOperationException("set failed");
        var sut = CreateSut(redis);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PublishAsync(ValidKline(), CancellationToken.None));

        Assert.Empty(redis.StringSets);
        Assert.Empty(redis.Publishes);
    }

    [Fact]
    public async Task PublishAsync_PublishFails_LatestStateWasAlreadyWritten()
    {
        var redis = RedisHarness.Create();
        redis.PublishException = new InvalidOperationException("publish failed");
        var sut = CreateSut(redis);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PublishAsync(ValidKline(), CancellationToken.None));

        Assert.Single(redis.StringSets);
        Assert.Empty(redis.Publishes);
    }

    [Fact]
    public async Task PublishAsync_CancelledToken_ThrowsBeforeRedisCalls()
    {
        var redis = RedisHarness.Create();
        var sut = CreateSut(redis);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.PublishAsync(ValidKline(), cts.Token));

        Assert.Empty(redis.StringSets);
        Assert.Empty(redis.Publishes);
    }

    public static IEnumerable<object[]> InvalidPayloads()
    {
        yield return [Kline("", "1m", "60000", "60150", "59950", "60100", "1")];
        yield return [Kline("BTCUSDC", "", "60000", "60150", "59950", "60100", "1")];
        yield return [Raw("""{"s":"BTCUSDC","i":"1m","t":2000,"T":1000,"o":"60000","h":"60150","l":"59950","c":"60100","v":"1"}""")];
        yield return [Kline("BTCUSDC", "1m", "60000", "59900", "60100", "60050", "1")];
        yield return [Kline("BTCUSDC", "1m", "60200", "60150", "59950", "60100", "1")];
        yield return [Kline("BTCUSDC", "1m", "60000", "60150", "59950", "60200", "1")];
        yield return [Kline("BTCUSDC", "1m", "60000", "60150", "59950", "60100", "-1")];
        yield return [Kline("BTCUSDC", "1m", "not-a-number", "60150", "59950", "60100", "1")];
        yield return [Raw("""{"s":"BTCUSDC","i":"1m","t":1000,"T":2000,"o":"60000","h":"60150","l":"59950","c":"60100"}""")];
    }

    private static KlinePublisher CreateSut(RedisHarness redis, int ttlSeconds = 86_400)
        => new(redis.Multiplexer, Options.Create(new MarketDataOptions { LatestKlineTtlSeconds = ttlSeconds }), NullLogger<KlinePublisher>.Instance);

    private static JsonElement ValidKline() => Kline("BTCUSDC", "1m", "60000", "60150", "59950", "60100", "23.4");

    private static JsonElement Kline(string symbol, string interval, string open, string high, string low, string close, string volume)
        => Raw($$"""{"s":{{JsonSerializer.Serialize(symbol)}},"i":{{JsonSerializer.Serialize(interval)}},"t":1000,"T":2000,"o":{{JsonSerializer.Serialize(open)}},"h":{{JsonSerializer.Serialize(high)}},"l":{{JsonSerializer.Serialize(low)}},"c":{{JsonSerializer.Serialize(close)}},"v":{{JsonSerializer.Serialize(volume)}}}""");

    private static JsonElement Raw(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class RedisHarness
    {
        private readonly RedisDispatchProxy _databaseProxy;
        private readonly RedisDispatchProxy _subscriberProxy;

        private RedisHarness(IConnectionMultiplexer multiplexer, RedisDispatchProxy databaseProxy, RedisDispatchProxy subscriberProxy)
        {
            Multiplexer = multiplexer;
            _databaseProxy = databaseProxy;
            _subscriberProxy = subscriberProxy;
        }

        public IConnectionMultiplexer Multiplexer { get; }
        public List<StringSetCall> StringSets => _databaseProxy.StringSets;
        public List<PublishCall> Publishes => _subscriberProxy.Publishes;
        public Exception? StringSetException { set => _databaseProxy.StringSetException = value; }
        public Exception? PublishException { set => _subscriberProxy.PublishException = value; }

        public static RedisHarness Create()
        {
            var database = DispatchProxy.Create<IDatabase, RedisDispatchProxy>();
            var subscriber = DispatchProxy.Create<ISubscriber, RedisDispatchProxy>();
            var multiplexer = DispatchProxy.Create<IConnectionMultiplexer, RedisDispatchProxy>();

            var databaseProxy = (RedisDispatchProxy)(object)database;
            var subscriberProxy = (RedisDispatchProxy)(object)subscriber;
            var multiplexerProxy = (RedisDispatchProxy)(object)multiplexer;
            multiplexerProxy.Database = database;
            multiplexerProxy.Subscriber = subscriber;

            return new RedisHarness(multiplexer, databaseProxy, subscriberProxy);
        }
    }

    private class RedisDispatchProxy : DispatchProxy
    {
        public IDatabase? Database { get; set; }
        public ISubscriber? Subscriber { get; set; }
        public Exception? StringSetException { get; set; }
        public Exception? PublishException { get; set; }
        public List<StringSetCall> StringSets { get; } = [];
        public List<PublishCall> Publishes { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var method = targetMethod ?? throw new InvalidOperationException("Missing target method.");
            args ??= [];

            if (method.Name == nameof(IConnectionMultiplexer.GetDatabase))
                return Database;

            if (method.Name == nameof(IConnectionMultiplexer.GetSubscriber))
                return Subscriber;

            if (method.Name == nameof(IDatabase.StringSetAsync) && method.ReturnType == typeof(Task<bool>))
            {
                if (StringSetException is not null)
                    return Task.FromException<bool>(StringSetException);

                StringSets.Add(new StringSetCall(
                    ((RedisKey)args[0]!).ToString(),
                    ((RedisValue)args[1]!).ToString(),
                    args.Length > 2 ? (TimeSpan?)args[2] : null));

                return Task.FromResult(true);
            }

            if (method.Name == nameof(ISubscriber.PublishAsync) && method.ReturnType == typeof(Task<long>))
            {
                if (PublishException is not null)
                    return Task.FromException<long>(PublishException);

                Publishes.Add(new PublishCall(((RedisChannel)args[0]!).ToString(), ((RedisValue)args[1]!).ToString()));
                return Task.FromResult(1L);
            }

            if (method.ReturnType == typeof(bool))
                return false;

            if (method.ReturnType == typeof(int))
                return 0;

            if (method.ReturnType == typeof(long))
                return 0L;

            if (method.ReturnType == typeof(string))
                return string.Empty;

            if (method.ReturnType == typeof(Task))
                return Task.CompletedTask;

            if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = method.ReturnType.GetGenericArguments()[0];
                var defaultValue = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [defaultValue]);
            }

            return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
        }
    }

    private sealed record StringSetCall(string Key, string Value, TimeSpan? Expiry);
    private sealed record PublishCall(string Channel, string Value);
}
