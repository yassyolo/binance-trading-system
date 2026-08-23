using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Indicators.Alligator;
using TradingSystem.Indicators.Alligator.Configuration;
using TradingSystem.Indicators.Bollinger;
using TradingSystem.Indicators.Bollinger.Configuration;
using TradingSystem.Indicators.Contracts;

namespace TradingSystem.Indicators;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingIndicators(this IServiceCollection services, IConfiguration configuration)
    {
        var alligator = configuration.GetSection(AlligatorOptions.SectionName).Get<AlligatorOptions>() ?? new();
        services.AddOptions<AlligatorOptions>().Bind(configuration.GetSection(AlligatorOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<AlligatorOptions>, AlligatorOptionsValidator>();

        var bollinger = configuration.GetSection(BollingerOptions.SectionName).Get<BollingerOptions>() ?? new();
        services.AddOptions<BollingerOptions>().Bind(configuration.GetSection(BollingerOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<BollingerOptions>, BollingerOptionsValidator>();

        if (alligator.Enabled)
            services.AddSingleton<IIndicatorProcessor, AlligatorIndicatorProcessor>();

        if (bollinger.Enabled)
            services.AddSingleton<IIndicatorProcessor, BollingerIndicatorProcessor>();

        return services;
    }
}