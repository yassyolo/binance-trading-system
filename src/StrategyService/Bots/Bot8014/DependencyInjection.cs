using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8014.Configuration;
using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Healing.Contracts;
using TradingSystem.Application.Orders;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Strategies.Contracts;
using TradingSystem.Binance.Startup;
using TradingSystem.Strategies.Grid;

namespace StrategyService.Bots.Bot8014;

public static class DependencyInjection
{
    public static IServiceCollection AddBot8014(this IServiceCollection s, IConfiguration c)
    {
        s.AddOptions<Bot8014Options>()
            .Bind(c.GetSection(Bot8014Options.SectionName))
            .ValidateOnStart();
        s.AddSingleton<IValidateOptions<Bot8014Options>, Bot8014OptionsValidator>();
        s.AddSingleton<IBinanceTradingConfiguration>(sp => sp.GetRequiredService<IOptions<Bot8014Options>>().Value);

        s.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Bot8014Options>>().Value;
            var gridSpacingPolicy = sp.GetRequiredService<GridSpacingPolicy>();
            return new TpOnlyGridGapPolicy<Bot8014Options>(options, gridSpacingPolicy);
        });

        s.AddSingleton<ITradingStrategy, Bot8014Strategy>();
        s.AddSingleton<IBotTradeExecutor, Bot8014TradeExecutor>();
        s.AddSingleton<IBotActivePositionProvider, Bot8014ActivePositionProvider>();
        s.AddSingleton<IBotOrderEventHandler, Bot8014OrderEventHandler>();
        s.AddSingleton<IBotHealingService, Bot8014HealingService>();
        return s;
    }
}
