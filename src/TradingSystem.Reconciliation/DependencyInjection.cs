using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Reconciliation.Configuration;
using TradingSystem.Reconciliation.Services;

namespace TradingSystem.Reconciliation;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingReconciliation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ReconciliationOptions>().Bind(configuration.GetSection(ReconciliationOptions.SectionName)).ValidateOnStart();

        services.AddSingleton<IValidateOptions<ReconciliationOptions>, ReconciliationOptionsValidator>();
        services.AddSingleton<PositionReconciliationService>();
       
        return services;
    }
}
