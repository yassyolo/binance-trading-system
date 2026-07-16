using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; // Add this using directive
using TradingSystem.Signals.Abstractions;

namespace StrategyService.Bot8012.Signals.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBot8012SignalGeneration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<Bot8012SignalGeneratorOptions>()
            .Configure(Bot8012SignalGeneratorOptions.SectionName, configuration) // Use BindConfiguration instead of Bind
            .Validate(x => !string.IsNullOrWhiteSpace(x.BotName), "BotName is required.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Symbol), "Symbol is required.")
            .ValidateOnStart();

        services.AddSingleton<ITradingSignalGenerator, Bot8012SignalGenerator>();
        return services;
    }
}
