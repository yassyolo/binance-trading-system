using TradingSystem.Application.Locking;
using TradingSystem.Redis.Positions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;
using TradingSystem.Redis.Configuration;
using TradingSystem.Redis.Engine;
using TradingSystem.Redis.Events;
using TradingSystem.Application.Events;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Redis.Messaging;
using TradingSystem.Redis.Signals;

namespace TradingSystem.Redis.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingRedis(this IServiceCollection services, IConfiguration configuration, bool subscribeToSignals = false)
    {
        services.AddOptions<RedisOptions>().Bind(configuration.GetSection(RedisOptions.SectionName)).Validate(x => !string.IsNullOrWhiteSpace(x.ConnectionString), "Redis connection string is required.").ValidateOnStart();
        services.AddSingleton<IConnectionMultiplexer>(sp => {var o = sp.GetRequiredService<IOptions<RedisOptions>>().Value;var c = ConfigurationOptions.Parse(o.ConnectionString);c.AbortOnConnectFail = o.AbortOnConnectFail;return ConnectionMultiplexer.Connect(c);});
        services.AddSingleton(sp => new RedisKeyFactory(sp.GetRequiredService<IOptions<RedisOptions>>().Value.KeyPrefix));
        services.AddSingleton<IPositionStore, RedisPositionStore>();services.AddSingleton<IPositionLockProvider, RedisPositionLockProvider>();
        services.AddSingleton<IEventDeduplicationStore, RedisEventDeduplicationStore>();
        services.AddSingleton<ISignalPublisher, RedisGeneratedSignalPublisher>();services.AddSingleton<IDistributedSignalThrottleStore, RedisSignalThrottleStore>();
        services.AddSingleton<ISignalCooldownStore, RedisSignalCooldownStore>();services.AddSingleton<ISignalIdempotencyStore, RedisSignalIdempotencyStore>();services.AddSingleton<ITradingOperationLockProvider, RedisTradingOperationLockProvider>();services.AddSingleton<IRedisMessagePublisher, RedisMessagePublisher>();services.AddSingleton<IRedisStatePublisher, RedisStatePublisher>();
        if(subscribeToSignals)services.AddHostedService<RedisTradingSignalSubscriber>();return services;
    }
}
