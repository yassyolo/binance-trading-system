using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Bot8014;

public sealed class Bot8014BacktestEngine(TpOnlyGridBacktestEngine engine)
{
    public BotBacktestResult<Bot8014BacktestOptions> Run(IReadOnlyList<MarketCandle> candles, IReadOnlyList<HistoricalBotSignal> signals, Bot8014BacktestOptions options) 
        => engine.Run(candles, signals, options);
}