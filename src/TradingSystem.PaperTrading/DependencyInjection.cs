using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.PaperTrading.Executor;
using TradingSystem.PaperTrading.Position;

namespace TradingSystem.PaperTrading.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddPaperTrading(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PaperTradingOptions>().Bind(configuration.GetSection(PaperTradingOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<PaperTradingOptions>, PaperTradingOptionsValidator>();

        services.TryAddSingleton<ITradingSignalContextAccessor, TradingSignalContextAccessor>();
        services.AddSingleton<PaperTradeExecutor>();
        services.AddSingleton<PaperActivePositionProvider>();

        services.RemoveAll<ITradeExecutor>();
        services.AddSingleton<ITradeExecutor, EnvironmentAwareTradeExecutor>();

        services.RemoveAll<IActivePositionProvider>();
        services.AddSingleton<IActivePositionProvider, EnvironmentAwareActivePositionProvider>();

        return services;
    }
}
