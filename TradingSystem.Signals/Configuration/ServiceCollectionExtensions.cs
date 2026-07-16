using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Services;

namespace TradingSystem.Signals.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingSignals(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SignalGenerationOptions>(
            configuration.GetSection(SignalGenerationOptions.SectionName));

        services.AddSingleton<ISignalPublisher, RedisSignalPublisher>();
        services.AddSingleton<ISignalGenerationCoordinator, SignalGenerationCoordinator>();
        return services;
    }
}
