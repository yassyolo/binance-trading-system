using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Operations.Configuration;
using TradingSystem.Operations.Workers;

namespace TradingSystem.Operations;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceHeartbeat(this IServiceCollection services, IConfiguration c, string serviceName)
    {
        services
       .AddOptions<ServiceHeartbeatOptions>()
       .Bind(c.GetSection(ServiceHeartbeatOptions.SectionName))
       .Configure(options => options.ServiceName = serviceName)
       .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<ServiceHeartbeatOptions>,
            ServiceHeartbeatOptionsValidator>();


        services.AddHostedService<ServiceHeartbeatWorker>();
        
        return services;
    }
    
    public static IServiceCollection AddAlertEngine(this IServiceCollection services, IConfiguration c)
    {
        services
        .AddOptions<AlertEngineOptions>()
        .Bind(c.GetSection(AlertEngineOptions.SectionName))
        .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<AlertEngineOptions>,
            AlertEngineOptionsValidator>();
        services.AddHostedService<AlertEngineWorker>();
        
        return services;
    }
}
