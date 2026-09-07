using System.Text.Json;
using TradingSystem.Contracts.Indicators;
using TradingSystem.Contracts.Klines;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.Signals;
using TradingSystem.Domain.Enums;
using TradingSystem.Infrastructure.Serialization;
using TradingSystem.Redis.Configuration;
using TradingSystem.Redis.Constants;
using Xunit;

namespace TradingSystem.Redis.Tests;

public sealed class RedisContractsAndKeyFactoryTests
{
    [Fact]
    public void Kline_NormalizesIntervalAndSymbol()
    {
        var result = RedisChannels.Kline(" 5M ", " btcusdc ");

        Assert.Equal("futures_kline_channel:5m:BTCUSDC", result);
    }

    [Fact]
    public void Indicator_NormalizesName()
    {
        var result = RedisChannels.Indicator(" Alligator_MA ");

        Assert.Equal("indicator_channel:alligator_ma", result);
    }

    [Fact]
    public void StrategySignals_HasStableChannelName()
    {
        Assert.Equal("trading:signals", RedisChannels.StrategySignals);
    }

    [Fact]
    public void KeyFactory_WithoutPrefix_NormalizesCooldownKey()
    {
        var sut = new RedisKeyFactory(string.Empty);

        var key = sut.Cooldown(" bot8012 ", " btcusdc ", PositionSide.Long);

        Assert.Equal("trading:cooldown:BOT8012:BTCUSDC:LONG", key.ToString());
    }

    [Theory]
    [InlineData("demo", "demo:trading:cooldown:BOT8012:BTCUSDC:SHORT")]
    [InlineData(" demo: ", "demo:trading:cooldown:BOT8012:BTCUSDC:SHORT")]
    public void KeyFactory_WithPrefix_AddsSingleSeparator(string prefix, string expected)
    {
        var sut = new RedisKeyFactory(prefix);

        var key = sut.Cooldown("BOT8012", "BTCUSDC", PositionSide.Short);

        Assert.Equal(expected, key.ToString());
    }

    [Fact]
    public void KeyFactory_SignalIdempotency_TrimsButPreservesIdentifierCase()
    {
        var sut = new RedisKeyFactory("paper");

        var key = sut.SignalIdempotency(" AbC-123 ");

        Assert.Equal("paper:trading:signal-idempotency:AbC-123", key.ToString());
    }

    [Fact]
    public void KeyFactory_OperationLock_NormalizesResourceIdentity()
    {
        var sut = new RedisKeyFactory(string.Empty);

        var key = sut.OperationLock(" bot8012 ", " btcusdc ", PositionSide.Short);

        Assert.Equal("trading:operation-lock:BOT8012:BTCUSDC:SHORT", key.ToString());
    }

    [Fact]
    public void KeyFactory_Position_NormalizesBotAndTrimsShortId()
    {
        var sut = new RedisKeyFactory("demo");

        var key = sut.Position(" bot8012 ", " abc123 ");

        Assert.Equal("demo:BOT8012:position:abc123", key.ToString());
    }

    [Fact]
    public void KeyFactory_PositionIndex_NormalizesBot()
    {
        var sut = new RedisKeyFactory(string.Empty);

        var key = sut.PositionIndex(" bot8012 ");

        Assert.Equal("BOT8012:positions", key.ToString());
    }

    [Fact]
    public void ClosedKlineMessage_SerializesUsingStableWireNames()
    {
        var message = new ClosedKlineMessage("BTCUSDC", 1000, "100", "110", "90", "105", "12.5", 1999, "5m");

        var json = JsonSerializer.Serialize(message, JsonDefaults.Messaging);

        Assert.Contains("\"symbol\":\"BTCUSDC\"", json);
        Assert.Contains("\"close_time\":1999", json);
        Assert.Contains("\"interval\":\"5m\"", json);
    }

    [Fact]
    public void TradingSignalMessage_SerializesUsingStableWireNames()
    {
        var generatedAt = new DateTime(2026, 1, 1, 12, 30, 0, DateTimeKind.Utc);
        var message = new TradingSignalMessage("S-1", "BOT8012", "BTCUSDC", "LONG", "Internal", generatedAt);

        var json = JsonSerializer.Serialize(message, JsonDefaults.Messaging);

        Assert.Contains("\"signal_id\":\"S-1\"", json);
        Assert.Contains("\"bot_name\":\"BOT8012\"", json);
        Assert.Contains("\"generated_at_utc\"", json);
    }

    [Fact]
    public void IndicatorSnapshotMessage_SerializesPreviousValueAndMetadata()
    {
        var message = new IndicatorSnapshotMessage
        {
            Type = "bb",
            Symbol = "BTCUSDC",
            Timeframe = "30m",
            CandleOpenTime = 1000,
            CandleCloseTime = 2000,
            PublishedAt = 2100,
            Indicators = new Dictionary<string, IndicatorValueMessage>
            {
                ["bb.upper"] = new()
                {
                    Value = 60200m,
                    PreviousValue = 60150m,
                    Metadata = new Dictionary<string, string>
                    {
                        ["length"] = "20",
                        ["source"] = "close"
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(message, JsonDefaults.Messaging);

        Assert.Contains("\"previous_value\":60150", json);
        Assert.Contains("\"metadata\"", json);
        Assert.Contains("\"bb.upper\"", json);
    }

    [Fact]
    public void MessagingJson_DeserializesPropertyNamesCaseInsensitively()
    {
        const string json = """
        {
          "SIGNAL_ID": "S-1",
          "BOT_NAME": "BOT8012",
          "SYMBOL": "BTCUSDC",
          "ACTION": "LONG",
          "SOURCE": "Internal",
          "GENERATED_AT_UTC": "2026-01-01T12:00:00Z"
        }
        """;

        var result = JsonSerializer.Deserialize<TradingSignalMessage>(json, JsonDefaults.Messaging);

        Assert.NotNull(result);
        Assert.Equal("S-1", result!.SignalId);
        Assert.Equal("BOT8012", result.BotName);
    }

    [Fact]
    public void MessagingJson_OmitsNullOptionalValues()
    {
        var message = new IndicatorValueMessage { Value = 10m, PreviousValue = null, Metadata = null };

        var json = JsonSerializer.Serialize(message, JsonDefaults.Messaging);

        Assert.DoesNotContain("previous_value", json);
        Assert.DoesNotContain("metadata", json);
    }

    [Fact]
    public void RedisOptions_DefaultsAreResilientForLocalDevelopment()
    {
        var options = new RedisOptions();

        Assert.Equal("localhost:6379", options.ConnectionString);
        Assert.False(options.AbortOnConnectFail);
        Assert.Equal(5, options.ConnectRetry);
        Assert.Equal(5000, options.ConnectTimeoutMilliseconds);
        Assert.Equal(30, options.KeepAliveSeconds);
    }
}
