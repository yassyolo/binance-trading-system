using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine;
using TradingSystem.Application.MarketData;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Exchange;
using TradingSystem.Binance.Execution;
using TradingSystem.Binance.Market;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Startup;

namespace TradingSystem.Binance.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBinanceFutures(this IServiceCollection services,  IConfiguration configuration)
    {
        services.AddOptions<BinanceFuturesOptions>()
            .Bind(configuration.GetSection(BinanceFuturesOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BinanceFuturesOptions>,  BinanceFuturesOptionsValidator>();

        services.AddHttpClient<IHistoricalCandleSource,  BinanceHistoricalCandleSource>()
            .ConfigureHttpClient((sp,  client)  =>  client.BaseAddress  =  new Uri(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BinanceFuturesOptions>>().Value.BaseUrl));

        services.AddHttpClient<IBinanceFuturesMarketClient,  BinanceFuturesMarketClient>((sp,  client)  => 
        {
            var options  =  sp.GetRequiredService<IOptions<BinanceFuturesOptions>>().Value;
            client.BaseAddress  =  new Uri(options.BaseUrl);
            client.Timeout  =  TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IBinanceFuturesOrderClient,  BinanceFuturesOrderClient>((sp,  client)  => 
        {
            var options  =  sp.GetRequiredService<IOptions<BinanceFuturesOptions>>().Value;
            client.BaseAddress  =  new Uri(options.BaseUrl);
            client.Timeout  =  TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IMarketPriceProvider,  BinanceMarketPriceProvider>();
        services.AddSingleton<BinanceExchangeInfoService>();
        services.AddSingleton<SafeBinanceOrderService>();
        services.AddHostedService<BinanceStartupService>();
        services.AddSingleton<BinanceTpOnlyPositionService>();
        services.AddSingleton<BinanceProtectedPositionService>();
        return services;
    }
}
