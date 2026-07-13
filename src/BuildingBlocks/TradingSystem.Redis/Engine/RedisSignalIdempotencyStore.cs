using StackExchange.Redis;
using TradingSystem.Application.Engine;

namespace TradingSystem.Redis.Engine;

public sealed class RedisSignalIdempotencyStore(
    IConnectionMultiplexer connectionMultiplexer)
    : ISignalIdempotencyStore
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public Task<bool> TryStartAsync(
        string signalId,
        TimeSpan processingTtl,
        CancellationToken cancellationToken)
    {
        return _database.StringSetAsync(
            GetKey(signalId),
            "processing",
            processingTtl,
            When.NotExists);
    }

    public Task MarkCompletedAsync(
        string signalId,
        TimeSpan completedTtl,
        CancellationToken cancellationToken)
    {
        return _database.StringSetAsync(
            GetKey(signalId),
            "completed",
            completedTtl);
    }

    public Task ReleaseAsync(
        string signalId,
        CancellationToken cancellationToken)
    {
        return _database.KeyDeleteAsync(GetKey(signalId));
    }

    private static RedisKey GetKey(string signalId)
        => $"trading:signal-idempotency:{signalId}";
}
