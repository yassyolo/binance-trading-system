using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Risk;
using TradingSystem.Application.Risk.Contracts;
using TradingSystem.PortfolioManagement;
using TradingSystem.RiskManagement.Configuration;
using TradingSystem.RiskManagement.Contracts;
using TradingSystem.RiskManagement.Services;

namespace TradingSystem.RiskManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddCentralRiskManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPortfolioManagement(configuration);

        services.AddOptions<CentralRiskOptions>().Bind(configuration.GetSection(CentralRiskOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<CentralRiskOptions>, CentralRiskOptionsValidator>();

        services.TryAddSingleton<IRiskStateProvider, EmptyRiskStateProvider>();
        services.TryAddSingleton<IRiskOrderSizingProvider, ConfiguredRiskOrderSizingProvider>();
        services.AddSingleton<IRiskAdmissionReservationStore, InMemoryRiskAdmissionReservationStore>();

        services.RemoveAll<ICentralRiskManager>();
        services.RemoveAll<IRiskAdmissionLifecycle>();

        services.AddSingleton<CentralRiskManager>();
        services.AddSingleton<ICentralRiskManager>(sp => sp.GetRequiredService<CentralRiskManager>());
        services.AddSingleton<IRiskAdmissionLifecycle>(sp => sp.GetRequiredService<CentralRiskManager>());

        return services;
    }
}
