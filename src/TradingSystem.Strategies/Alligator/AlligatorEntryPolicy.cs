using TradingSystem.Domain.Enums;
using TradingSystem.Strategies.Alligator.Models;

namespace TradingSystem.Strategies.Alligator;

public sealed class AlligatorEntryPolicy
{
    public AlligatorEntryDecision? Evaluate(AlligatorEntryInput input, AlligatorEntryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        if (string.IsNullOrWhiteSpace(parameters.Symbol))
            throw new ArgumentException("Strategy symbol is required.", nameof(parameters));

        if (string.IsNullOrWhiteSpace(parameters.Interval))
            throw new ArgumentException("Strategy interval is required.", nameof(parameters));

        if (parameters.MinimumCandleRange < 0)
            throw new ArgumentOutOfRangeException(nameof(parameters), "Minimum candle range cannot be negative.");

        if (!input.IsClosed ||
            !input.Symbol.Equals(parameters.Symbol, StringComparison.OrdinalIgnoreCase) ||
            !input.Interval.Equals(parameters.Interval, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var candleRange = input.High - input.Low;
        if (candleRange < parameters.MinimumCandleRange)
            return null;

        var bullishCross = input.Close > input.Open &&
                           input.Open < input.Teeth &&
                           input.Close > input.Teeth;

        if (bullishCross &&
            parameters.EnableLong &&
            (!parameters.UseMa200Filter || input.Close >= input.Sma200))
        {
            return new AlligatorEntryDecision(PositionSide.Long, $"Bullish candle crossed Teeth. Close = {input.Close}, Teeth = {input.Teeth}, SMA200 = {input.Sma200}.");
        }

        var bearishCross = input.Close < input.Open &&
                           input.Open > input.Teeth &&
                           input.Close < input.Teeth;

        if (bearishCross &&
            parameters.EnableShort &&
            (!parameters.UseMa200Filter || input.Close <= input.Sma200))
        {
            return new AlligatorEntryDecision(PositionSide.Short, $"Bearish candle crossed Teeth. Close = {input.Close}, Teeth = {input.Teeth}, SMA200 = {input.Sma200}.");
        }

        return null;
    }
}
