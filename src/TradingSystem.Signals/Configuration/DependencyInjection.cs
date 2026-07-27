using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Services;

namespace TradingSystem.Signals.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingSignals(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SignalGenerationOptions>()
            .Bind(configuration.GetSection(SignalGenerationOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SignalGenerationOptions>, SignalGenerationOptionsValidator>();
        services.AddSingleton<ISignalGenerationCoordinator, SignalGenerationCoordinator>();
        return services;
    }
}
