using Microsoft.Extensions.Options;
using AlligatorIndicatorService.Services;

namespace AlligatorIndicatorService.Configuration;

public static class AlligatorServiceCollectionExtensions
{
    public static IServiceCollection AddAlligatorIndicator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AlligatorOptions>()
            .Bind(configuration.GetSection(AlligatorOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<AlligatorOptions>,
            AlligatorOptionsValidator>();

        services.AddHttpClient<BinanceHistoricalKlineClient>();
        services.AddSingleton<AlligatorMaEngine>();

        services.AddHostedService<AlligatorHistoryInitializer>();
        services.AddHostedService<AlligatorRedisSubscriber>();

        return services;
    }
}
