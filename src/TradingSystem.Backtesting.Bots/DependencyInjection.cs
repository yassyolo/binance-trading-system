using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Backtesting.Bots.Bot8011;
using TradingSystem.Backtesting.Bots.Bot8012;
using TradingSystem.Backtesting.Bots.Bot8013;
using TradingSystem.Backtesting.Bots.Bot8014;
using TradingSystem.Backtesting.Bots.Bot8015;
using TradingSystem.Backtesting.Bots.Bot8016;
using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Strategies.Alligator;
using TradingSystem.Strategies.Grid;
using TradingSystem.Strategies.Protection;

namespace TradingSystem.Backtesting.Bots;

public static class DependencyInjection
{
    public static IServiceCollection AddBotBacktesting(this IServiceCollection s)
    {
        s.AddSingleton<GridSpacingPolicy>();
        s.AddSingleton<Stop3Policy>();
        s.AddSingleton<AlligatorEntryPolicy>();
        
        s.AddSingleton<TpOnlyGridBacktestEngine>();
        s.AddSingleton<Bot8011BacktestEngine>();
        s.AddSingleton<Bot8012BacktestEngine>();
        s.AddSingleton<Bot8013BacktestEngine>();
        s.AddSingleton<Bot8014BacktestEngine>();
        s.AddSingleton<Bot8015BacktestEngine>();
        s.AddSingleton<Bot8016BacktestEngine>();
        
        return s;
    }
}