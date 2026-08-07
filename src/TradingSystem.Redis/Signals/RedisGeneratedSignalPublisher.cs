using TradingSystem.Contracts.Messaging;
using TradingSystem.Contracts.Signals;
using TradingSystem.Redis.Messaging.Contracts;
using TradingSystem.Signals.Contracts;
using TradingSystem.Signals.Models;
namespace TradingSystem.Redis.Signals;
public sealed class RedisGeneratedSignalPublisher(
	IRedisMessagePublisher publisher)
	:ISignalPublisher
{
	public Task PublishAsync(GeneratedTradingSignal x, CancellationToken ct)
		=> publisher.PublishAsync(RedisChannels.StrategySignals, new TradingSignalMessage(x.SignalId, x.BotName, x.Symbol, x.Action, x.Source, x.GeneratedAtUtc), ct);
}
