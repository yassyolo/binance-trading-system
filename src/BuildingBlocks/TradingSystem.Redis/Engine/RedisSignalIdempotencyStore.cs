using StackExchange.Redis;
using TradingSystem.Application.Engine;

namespace TradingSystem.Redis.Engine;

public sealed class RedisSignalIdempotencyStore(IConnectionMultiplexer redis)
    : ISignalIdempotencyStore
{
    private readonly IDatabase _database = redis.GetDatabase();

    public Task<bool> TryStartAsync(string signalId, TimeSpan processingTtl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _database.StringSetAsync(
            RedisKeyFactory.SignalIdempotency(signalId),
            "processing",
            processingTtl,
            When.NotExists);
    }

    public Task MarkCompletedAsync(string signalId, TimeSpan completedTtl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _database.StringSetAsync(
            RedisKeyFactory.SignalIdempotency(signalId),
            "completed",
            completedTtl);
    }

    public Task ReleaseAsync(string signalId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _database.KeyDeleteAsync(RedisKeyFactory.SignalIdempotency(signalId));
    }
}
