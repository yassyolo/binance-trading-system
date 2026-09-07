using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.UserStream;
using TradingSystem.Redis.Messaging.Contracts;
using UserStreamService.Configuration;
using UserStreamService.Services;

namespace UserStreamService.Tests;

public sealed class UserStreamEventProcessorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_OrderTradeUpdate_PublishesRawAndOrderChannels()
    {
        var publisher = new CapturingPublisher();
        var sut = CreateSut(publisher, publishRaw: true);

        await sut.ProcessAsync("""{"e":"ORDER_TRADE_UPDATE","o":{"c":"BOT8012_TP_abc123"}}""", default);

        Assert.Equal(2, publisher.Messages.Count);
        Assert.Contains(publisher.Messages, x => x.Channel == RedisChannels.UserStreamRaw);
        Assert.Contains(publisher.Messages, x => x.Channel == RedisChannels.UserStreamOrder);

        var envelope = Assert.IsType<UserStreamEnvelope>(
            publisher.Messages.Single(x => x.Channel == RedisChannels.UserStreamOrder).Message);

        Assert.Equal(Now.UtcDateTime, envelope.HubTimestampUtc);
        Assert.Equal(1, envelope.HubSequence);
        Assert.Equal("ORDER_TRADE_UPDATE", envelope.Binance.GetProperty("e").GetString());
    }

    [Fact]
    public async Task ProcessAsync_AccountUpdate_PublishesAccountChannel()
    {
        var publisher = new CapturingPublisher();
        var sut = CreateSut(publisher, publishRaw: false);

        await sut.ProcessAsync("""{"e":"ACCOUNT_UPDATE","a":{"m":"ORDER"}}""", default);

        var item = Assert.Single(publisher.Messages);
        Assert.Equal(RedisChannels.UserStreamAccount, item.Channel);
    }

    [Theory]
    [InlineData("ORDER_TRADE_UPDATE")]
    [InlineData("ALGO_UPDATE")]
    [InlineData("TRADE_LITE")]
    public async Task ProcessAsync_OrderRelatedEvents_PublishOrderChannel(string eventType)
    {
        var publisher = new CapturingPublisher();
        var sut = CreateSut(publisher, publishRaw: false);

        await sut.ProcessAsync($$"""{"e":"{{eventType}}"}""", default);

        var item = Assert.Single(publisher.Messages);
        Assert.Equal(RedisChannels.UserStreamOrder, item.Channel);
    }

    [Fact]
    public async Task ProcessAsync_PublishRawFalse_DoesNotPublishRawCopy()
    {
        var publisher = new CapturingPublisher();
        var sut = CreateSut(publisher, publishRaw: false);

        await sut.ProcessAsync("""{"e":"ORDER_TRADE_UPDATE"}""", default);

        Assert.DoesNotContain(publisher.Messages, x => x.Channel == RedisChannels.UserStreamRaw);
        Assert.Single(publisher.Messages);
    }

    [Fact]
    public async Task ProcessAsync_InvalidJson_DoesNotPublish()
    {
        var publisher = new CapturingPublisher();
        var sut = CreateSut(publisher, publishRaw: true);

        await sut.ProcessAsync("{not-json", default);

        Assert.Empty(publisher.Messages);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"e":null}""")]
    [InlineData("""{"e":""}""")]
    public async Task ProcessAsync_MissingOrInvalidEventType_DoesNotPublish(string json)
    {
        var publisher = new CapturingPublisher();
        var sut = CreateSut(publisher, publishRaw: true);

        await sut.ProcessAsync(json, default);

        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task ProcessAsync_UnknownEvent_PublishesOnlyRawWhenEnabled()
    {
        var publisher = new CapturingPublisher();
        var sut = CreateSut(publisher, publishRaw: true);

        await sut.ProcessAsync("""{"e":"MARGIN_CALL"}""", default);

        var item = Assert.Single(publisher.Messages);
        Assert.Equal(RedisChannels.UserStreamRaw, item.Channel);
    }

    [Fact]
    public async Task ProcessAsync_ConsecutiveMessages_IncrementHubSequence()
    {
        var publisher = new CapturingPublisher();
        var sut = CreateSut(publisher, publishRaw: false);

        await sut.ProcessAsync("""{"e":"ACCOUNT_UPDATE"}""", default);
        await sut.ProcessAsync("""{"e":"ORDER_TRADE_UPDATE"}""", default);

        var envelopes = publisher.Messages.Select(x => Assert.IsType<UserStreamEnvelope>(x.Message)).ToArray();

        Assert.Equal(1, envelopes[0].HubSequence);
        Assert.Equal(2, envelopes[1].HubSequence);
    }

    private static UserStreamEventProcessor CreateSut(CapturingPublisher publisher, bool publishRaw)
        => new(
            publisher,
            new FixedTimeProvider(Now),
            Options.Create(new UserStreamServiceOptions { PublishRaw = publishRaw }),
            NullLogger<UserStreamEventProcessor>.Instance);

    private sealed class CapturingPublisher : IRedisMessagePublisher
    {
        public List<(string Channel, object Message)> Messages { get; } = [];

        public Task PublishAsync<T>(string channel, T message, CancellationToken ct = default)
        {
            Messages.Add((channel, message!));
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
