using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Domain.MarketData;
namespace TradingSystem.Backtesting.Bots.Bot8013;public sealed class Bot8013BacktestEngine(TpOnlyGridBacktestEngine engine){public BotBacktestResult<Bot8013BacktestOptions> Run(IReadOnlyList<MarketCandle> candles, IReadOnlyList<HistoricalBotSignal> signals, Bot8013BacktestOptions options) => engine.Run(candles, signals, options);}