using Microsoft.Extensions.Configuration;using Microsoft.Extensions.DependencyInjection;
namespace TradingSystem.Operations;
public static class DependencyInjection
{
 public static IServiceCollection AddServiceHeartbeat(this IServiceCollection services, IConfiguration c, string serviceName){services.Configure<ServiceHeartbeatOptions>(o => {c.GetSection(ServiceHeartbeatOptions.SectionName).Bind(o);o.ServiceName = serviceName;});services.AddHostedService<ServiceHeartbeatWorker>();return services;}
 public static IServiceCollection AddAlertEngine(this IServiceCollection services, IConfiguration c){services.Configure<AlertEngineOptions>(c.GetSection(AlertEngineOptions.SectionName));services.AddHostedService<AlertEngineWorker>();return services;}
}
