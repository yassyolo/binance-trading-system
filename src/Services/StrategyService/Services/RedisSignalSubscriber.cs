using System.Text.Json;
using StackExchange.Redis;
using TradingSystem.Contracts.Redis;
using TradingSystem.Domain.Signals;

namespace StrategyService.Services;

public sealed class RedisSignalSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly SignalProcessor _signalProcessor;
    private readonly ILogger<RedisSignalSubscriber> _logger;

    public RedisSignalSubscriber(
        IConnectionMultiplexer redis,
        SignalProcessor signalProcessor,
        ILogger<RedisSignalSubscriber> logger)
    {
        _redis = redis;
        _signalProcessor = signalProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal(RedisChannels.StrategySignals),
            async (_, message) =>
            {
                if (!message.HasValue)
                    return;

                try
                {
                    var signal = JsonSerializer.Deserialize<TradingSignal>(
                        message.ToString(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (signal is null)
                        return;

                    await _signalProcessor.ProcessAsync(signal, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Trading signal processing failed. Raw={Raw}", message.ToString());
                }
            });

        _logger.LogInformation(
            "Subscribed to strategy signal channel {Channel}",
            RedisChannels.StrategySignals);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}