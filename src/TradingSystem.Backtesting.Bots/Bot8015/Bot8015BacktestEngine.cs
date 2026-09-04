using TradingSystem.Backtesting.Bots.Bot8011;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Domain.MarketData;

namespace TradingSystem.Backtesting.Bots.Bot8015;

public sealed class Bot8015BacktestEngine(Bot8011BacktestEngine engine)
{
    public async Task<BotBacktestResult<Bot8015BacktestOptions>> RunAsync(IReadOnlyList<MarketCandle> candles, IReadOnlyList<HistoricalBotSignal> signals, Bot8015BacktestOptions o, CancellationToken ct = default)
    {
        var mapped = new Bot8011BacktestOptions
        {
            BotName = o.BotName, 
            Symbol = o.Symbol,
            Quantity = o.Quantity, 
            Leverage = o.Leverage, 
            InitialStopLossDistance = o.InitialStopLossDistance, 
            TakeProfitPercent = o.TakeProfitPercent, 
            TakeProfitCloseFraction = o.TakeProfitCloseFraction, 
            CooldownSeconds = o.CooldownSeconds, 
            EnableLong = o.EnableLong, 
            EnableShort = o.EnableShort, 
            Stop3EntryOffset = o.Stop3EntryOffset,
            Stop3TrailingStep = o.Stop3TrailingStep,
            Stop3TrailingBuffer = o.Stop3TrailingBuffer,
            TakerFeeRate = o.TakerFeeRate,
            SlippageBasisPoints = o.SlippageBasisPoints,
            MinimumQuantity = o.MinimumQuantity,
            QuantityStep = o.QuantityStep, 
            MinimumNotional = o.MinimumNotional,
            TickSize = o.TickSize, 
            EnterOnNextCandleOpen = o.EnterOnNextCandleOpen};
        
        var r = await engine.RunAsync(o.InitialBalance, mapped, candles, signals, ct);
        
        return new()
        {
            RunId = r.RunId.Replace("BOT8011", "BOT8015"), 
            BotName = o.BotName,
            Options = o, 
            StartedAtUtc = r.StartedAtUtc,
            CompletedAtUtc = r.CompletedAtUtc, 
            Metrics = r.Metrics, 
            Positions = r.Positions,
            Executions = r.Executions, 
            Decisions = r.Decisions, 
            EquityCurve = r.EquityCurve,
            Candles = r.Candles,
            Signals = r.Signals
        };
    }
}