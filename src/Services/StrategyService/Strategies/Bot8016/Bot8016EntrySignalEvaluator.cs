using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using StrategyService.Models;
using TradingSystem.Domain.Enums;

namespace StrategyService.Strategies.Bot8016;

public sealed class Bot8016EntrySignalEvaluator(
    IOptions<Bot8016Options> options)
{
    private readonly Bot8016Options _options = options.Value;

    public Bot8016EntrySignal? Evaluate(
        Bot8016Candle candle,
        Bot8016IndicatorSnapshot indicator)
    {
        if (!candle.IsClosed)
            return null;

        if (!candle.Symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase)
            || !candle.Interval.Equals(_options.EntryTimeframe, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!indicator.Symbol.Equals(candle.Symbol, StringComparison.OrdinalIgnoreCase)
            || !indicator.Timeframe.Equals(candle.Interval, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var range = candle.High - candle.Low;
        if (range < _options.MinimumSignalCandleRange)
            return null;

        var bullishCross =
            candle.Close > candle.Open
            && candle.Open < indicator.Teeth
            && candle.Close > indicator.Teeth;

        if (bullishCross
            && _options.EnableLong
            && (!_options.UseMa200Filter || candle.Close >= indicator.Sma200))
        {
            return new Bot8016EntrySignal(
                PositionSide.Long,
                candle,
                indicator,
                $"Bullish candle crossed Teeth. Close={candle.Close}, Teeth={indicator.Teeth}, SMA200={indicator.Sma200}.");
        }

        var bearishCross =
            candle.Close < candle.Open
            && candle.Open > indicator.Teeth
            && candle.Close < indicator.Teeth;

        if (bearishCross
            && _options.EnableShort
            && (!_options.UseMa200Filter || candle.Close <= indicator.Sma200))
        {
            return new Bot8016EntrySignal(
                PositionSide.Short,
                candle,
                indicator,
                $"Bearish candle crossed Teeth. Close={candle.Close}, Teeth={indicator.Teeth}, SMA200={indicator.Sma200}.");
        }

        return null;
    }
}
