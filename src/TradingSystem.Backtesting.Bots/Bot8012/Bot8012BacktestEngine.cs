using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Bot8012;

public sealed class Bot8012BacktestEngine(TpOnlyGridBacktestEngine tpOnlyGridEngine)
{
    public BotBacktestResult<Bot8012BacktestOptions> Run(IReadOnlyList<MarketCandle> candles, IReadOnlyList<HistoricalBotSignal> signals, Bot8012BacktestOptions options)
        => tpOnlyGridEngine.Run(candles, signals, options);
}
