using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TradingSystem.Application.Execution;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.PaperTrading.Executor;
using TradingSystem.PaperTrading.Position;
using TradingSystem.PaperTrading.Workers;

namespace TradingSystem.PaperTrading.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddPaperTrading(
        this IServiceCollection services,
        IConfiguration configuration,
        bool addFillWorker = true)
    {
        services.AddOptions<PaperTradingOptions>()
            .Bind(configuration.GetSection(PaperTradingOptions.SectionName))
            .Validate(x => x.InitialBalance > 0, "Paper initial balance must be positive.")
            .Validate(x => x.CommissionPercent >= 0 && x.SlippagePercent >= 0, "Paper costs cannot be negative.")
            .ValidateOnStart();

        services.TryAddSingleton<ITradingSignalContextAccessor, TradingSignalContextAccessor>();
        services.AddSingleton<PaperTradeExecutor>();
        services.AddSingleton<PaperActivePositionProvider>();

        services.RemoveAll<ITradeExecutor>();
        services.AddSingleton<ITradeExecutor, EnvironmentAwareTradeExecutor>();

        services.RemoveAll<IActivePositionProvider>();
        services.AddSingleton<IActivePositionProvider, EnvironmentAwareActivePositionProvider>();

        if (addFillWorker)
            services.AddHostedService<PaperFillWorker>();

        return services;
    }
}
