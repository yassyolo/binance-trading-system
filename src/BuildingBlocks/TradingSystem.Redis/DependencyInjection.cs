using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;
using TradingSystem.Redis.Configuration;
using TradingSystem.Redis.Engine;
using TradingSystem.Redis.Positions;
using TradingSystem.Redis.Subscribers;

namespace TradingSystem.Redis;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Redis connection string is required.")
            .ValidateOnStart();

        services
            .AddOptions<RedisPositionStoreOptions>()
            .Bind(configuration.GetSection(
                RedisPositionStoreOptions.SectionName));

        var redisOptions = configuration
            .GetSection(RedisOptions.SectionName)
            .Get<RedisOptions>()
            ?? new RedisOptions();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configurationOptions = ConfigurationOptions.Parse(
                redisOptions.ConnectionString);

            configurationOptions.AbortOnConnectFail = true;

            var connection = ConnectionMultiplexer.Connect(
                configurationOptions);

            connection.GetDatabase().Ping();

            return connection;
        });

        services.AddSingleton<IPositionStore, RedisPositionStore>();

        services.AddSingleton<
            ISignalCooldownStore,
            RedisSignalCooldownStore>();

        services.AddSingleton<
            ISignalIdempotencyStore,
            RedisSignalIdempotencyStore>();

        services.AddSingleton<
            ITradingOperationLockProvider,
            RedisTradingOperationLockProvider>();

        services.AddHostedService<RedisSignalSubscriber>();

        return services;
    }
}