using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.MarketData;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Exchange;
using TradingSystem.Binance.Execution;
using TradingSystem.Binance.Market;
using TradingSystem.Binance.Market.Contracts;
using TradingSystem.Binance.Orders;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Binance.Resilience;
using TradingSystem.Binance.Resilience.Configuration;
using TradingSystem.Binance.Startup;

namespace TradingSystem.Binance;

public static class DependencyInjection
{
    public static IServiceCollection AddBinancePublicMarketData(this IServiceCollection services, IConfiguration configuration)
    {
        AddCommonOptions(services, configuration);

        services.AddHttpClient<IHistoricalCandleSource, BinanceHistoricalCandleSource>().ConfigureHttpClient(ConfigureBinanceHttpClient);
        services.AddHttpClient<IHistoricalCandleRangeSource, BinanceHistoricalCandleRangeSource>().ConfigureHttpClient(ConfigureBinanceHttpClient);
        services.AddHttpClient<IBinanceFuturesMarketClient,BinanceFuturesMarketClient>().ConfigureHttpClient(ConfigureBinanceHttpClient);
        services.AddSingleton<IMarketPriceProvider, BinanceMarketPriceProvider>();
        services.AddSingleton<BinanceExchangeInfoService>();

        return services;
    }

    public static IServiceCollection AddBinanceFutures(this IServiceCollection services, IConfiguration configuration)
    {
        AddCommonOptions(services, configuration);

        services.AddHttpClient<IHistoricalCandleSource, BinanceHistoricalCandleSource>().ConfigureHttpClient(ConfigureBinanceHttpClient);

        services
            .AddHttpClient<
                IHistoricalCandleRangeSource,
                BinanceHistoricalCandleRangeSource>()
            .ConfigureHttpClient(ConfigureBinanceHttpClient);

        services
            .AddHttpClient<
                IBinanceFuturesMarketClient,
                BinanceFuturesMarketClient>()
            .ConfigureHttpClient(ConfigureBinanceHttpClient);

        services
            .AddHttpClient<
                IBinanceFuturesOrderClient,
                BinanceFuturesOrderClient>()
            .ConfigureHttpClient(ConfigureBinanceHttpClient);

        services.AddSingleton<
            IMarketPriceProvider,
            BinanceMarketPriceProvider>();

        services.AddSingleton<BinanceExchangeInfoService>();
        services.AddSingleton<SafeBinanceOrderService>();

        services.AddHostedService<BinanceStartupService>();

        services.AddSingleton<BinanceTpOnlyPositionService>();
        services.AddSingleton<BinanceProtectedPositionService>();

        return services;
    }

    private static void AddCommonOptions(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<BinanceFuturesOptions>()
            .Bind(configuration.GetSection(
                BinanceFuturesOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<BinanceFuturesOptions>,
            BinanceFuturesOptionsValidator>();

        services
            .AddOptions<BinanceRetryOptions>()
            .Bind(configuration.GetSection(
                BinanceRetryOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<BinanceRetryOptions>,
            BinanceRetryOptionsValidator>();

        services.AddSingleton<BinanceRetryService>();
    }

    private static void ConfigureBinanceHttpClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<BinanceFuturesOptions>>().Value;

        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(30);
    }
}