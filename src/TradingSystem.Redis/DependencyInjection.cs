using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Events;
using TradingSystem.Application.Locking;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Redis.Configuration;
using TradingSystem.Redis.Constants;
using TradingSystem.Redis.Engine;
using TradingSystem.Redis.Events;
using TradingSystem.Redis.Messaging;
using TradingSystem.Redis.Messaging.Contracts;
using TradingSystem.Redis.Positions;
using TradingSystem.Redis.Signals;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Contracts;

namespace TradingSystem.Redis;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingRedis(this IServiceCollection services,  IConfiguration configuration, bool subscribeToSignals = false)
    {
        services.AddOptions<RedisOptions>().Bind(configuration.GetSection(RedisOptions.SectionName)).Validate(x => !string.IsNullOrWhiteSpace(x.ConnectionString), "Redis connection string is required.").ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("TradingSystem.Redis.Connection");
            var configurationOptions = ConfigurationOptions.Parse(options.ConnectionString);

            configurationOptions.AbortOnConnectFail = options.AbortOnConnectFail;
            configurationOptions.ConnectRetry = Math.Max(1, options.ConnectRetry);
            configurationOptions.ConnectTimeout = Math.Max(1_000, options.ConnectTimeoutMilliseconds);
            configurationOptions.SyncTimeout = Math.Max(1_000, options.SyncTimeoutMilliseconds);
            configurationOptions.AsyncTimeout = Math.Max(1_000, options.AsyncTimeoutMilliseconds);
            configurationOptions.KeepAlive = Math.Max(5, options.KeepAliveSeconds);
            configurationOptions.ReconnectRetryPolicy = new ExponentialRetry(1_000, 30_000);

            var connection = ConnectionMultiplexer.Connect(configurationOptions);

            connection.ConnectionFailed += (_, args) =>
                logger.LogWarning(
                    args.Exception,
                    "Redis connection failed. Endpoint = {Endpoint}, FailureType = {FailureType}",
                    args.EndPoint,
                    args.FailureType);

            connection.ConnectionRestored += (_, args) =>
                logger.LogInformation(
                    "Redis connection restored. Endpoint = {Endpoint}, FailureType = {FailureType}",
                    args.EndPoint,
                    args.FailureType);

            connection.ErrorMessage += (_, args) =>
                logger.LogWarning("Redis error: {Message}", args.Message);

            return connection;
        });
        services.AddSingleton(sp => new RedisKeyFactory(sp.GetRequiredService<IOptions<RedisOptions>>().Value.KeyPrefix));

        services.AddSingleton<IPositionStore, RedisPositionStore>();
        services.AddSingleton<IPositionLockProvider, RedisPositionLockProvider>();
        services.AddSingleton<IEventDeduplicationStore, RedisEventDeduplicationStore>();
        services.AddSingleton<ISignalPublisher, RedisGeneratedSignalPublisher>();
        services.AddSingleton<IDistributedSignalThrottleStore, RedisSignalThrottleStore>();
        services.AddSingleton<ISignalCooldownStore, RedisSignalCooldownStore>();
        services.AddSingleton<ISignalIdempotencyStore, RedisSignalIdempotencyStore>();
        services.AddSingleton<ITradingOperationLockProvider, RedisTradingOperationLockProvider>();
        services.AddSingleton<IRedisMessagePublisher, RedisMessagePublisher>();
        services.AddSingleton<IRedisStatePublisher, RedisStatePublisher>();

        if (subscribeToSignals)
            services.AddHostedService<RedisTradingSignalSubscriber>();

        return services;
    }
}
