using Microsoft.Extensions.Configuration;using Microsoft.Extensions.DependencyInjection;using TradingSystem.Indicators.Abstractions;using TradingSystem.Indicators.Alligator;using TradingSystem.Indicators.Bollinger;
namespace TradingSystem.Indicators;
public static class DependencyInjection
{
 public static IServiceCollection AddTradingIndicators(this IServiceCollection s, IConfiguration c){s.AddOptions<AlligatorOptions>().Bind(c.GetSection(AlligatorOptions.SectionName)).Validate(x => x.HistoryLimit>=x.SmaLength, "Alligator history must cover SMA").ValidateOnStart();s.AddOptions<BollingerOptions>().Bind(c.GetSection(BollingerOptions.SectionName)).Validate(x => x.Bands.Count>0 && x.Bands.All(b => b.Length>0), "Bollinger bands are invalid").ValidateOnStart();s.AddSingleton<IIndicatorProcessor, AlligatorIndicatorProcessor>();s.AddSingleton<IIndicatorProcessor, BollingerIndicatorProcessor>();return s;}
}
