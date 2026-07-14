using StackExchange.Redis;
using TradingSystem.Application.Engine;
using TradingSystem.Domain.Enums;

namespace TradingSystem.Redis.Engine;

public sealed class RedisTradingOperationLockProvider(IConnectionMultiplexer redis)
    : ITradingOperationLockProvider
{
    private const string ReleaseScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        end
        return 0
        """;

    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string botName,
        string symbol,
        PositionSide side,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ttl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl));

        var key = RedisKeyFactory.OperationLock(botName, symbol, side);
        var token = Guid.NewGuid().ToString("N");
        var acquired = await _database.StringSetAsync(key, token, ttl, When.NotExists);

        return acquired ? new Handle(_database, key, token) : null;
    }

    private sealed class Handle(IDatabase database, RedisKey key, RedisValue token) : IAsyncDisposable
    {
        private int _disposed;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            await database.ScriptEvaluateAsync(ReleaseScript, [key], [token]);
        }
    }
}
