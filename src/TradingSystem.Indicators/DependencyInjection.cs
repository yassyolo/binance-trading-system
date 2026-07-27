using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Indicators.Abstractions;
using TradingSystem.Indicators.Alligator;
using TradingSystem.Indicators.Bollinger;

namespace TradingSystem.Indicators;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingIndicators(this IServiceCollection services, IConfiguration configuration)
    {
        var alligator = configuration.GetSection(AlligatorOptions.SectionName).Get<AlligatorOptions>() ?? new();
        var bollinger = configuration.GetSection(BollingerOptions.SectionName).Get<BollingerOptions>() ?? new();

        services.AddOptions<AlligatorOptions>()
            .Bind(configuration.GetSection(AlligatorOptions.SectionName))
            .Validate(x => x.Symbols.Length > 0 && x.Intervals.Length > 0, "Alligator symbols and intervals are required.")
            .Validate(x => x.HistoryLimit >= x.SmaLength, "Alligator history must cover SMA.")
            .Validate(x => x.SmaLength > 0 && x.JawLength > 0 && x.TeethLength > 0 && x.LipsLength > 0, "Alligator lengths must be positive.")
            .ValidateOnStart();

        services.AddOptions<BollingerOptions>()
            .Bind(configuration.GetSection(BollingerOptions.SectionName))
            .Validate(x => x.Symbols.Length > 0 && x.Intervals.Length > 0, "Bollinger symbols and intervals are required.")
            .Validate(x => x.HistoryLimit > 0 && x.Bands.Count > 0, "Bollinger history and bands are required.")
            .Validate(x => x.Bands.All(b => !string.IsNullOrWhiteSpace(b.Name) && b.Length > 0 && b.Multiplier > 0), "Bollinger bands are invalid.")
            .Validate(x => x.Bands.Select(b => b.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == x.Bands.Count, "Bollinger band names must be unique.")
            .Validate(x => x.Bands.All(b => b.Source.Equals("open", StringComparison.OrdinalIgnoreCase) || b.Source.Equals("close", StringComparison.OrdinalIgnoreCase)), "Bollinger source must be open or close.")
            .Validate(x => x.HistoryLimit >= x.Bands.Max(b => b.Length), "Bollinger history must cover the longest band.")
            .ValidateOnStart();

        if (alligator.Enabled)
            services.AddSingleton<IIndicatorProcessor, AlligatorIndicatorProcessor>();

        if (bollinger.Enabled)
            services.AddSingleton<IIndicatorProcessor, BollingerIndicatorProcessor>();

        return services;
    }
}
