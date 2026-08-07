using System.Globalization;
using TradingSystem.Backtesting.Strategies.Contracts;

namespace TradingSystem.Backtesting.Strategies;

public sealed class EmaCrossStrategyFactory : IBacktestStrategyFactory
{
    public string Name => "EMA_CROSS";

    public IBacktestStrategy Create(IReadOnlyDictionary<string, string> parameters)
         => new EmaCrossStrategy(
            GetInt(parameters, "fast", 20),
            GetInt(parameters, "slow", 50),
            GetDecimal(parameters, "slPercent", 1m),
            GetDecimal(parameters, "tpPercent", 2m));

    private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
         => values.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result : fallback;

    private static decimal GetDecimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback)
         => values.TryGetValue(key, out var value) && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result : fallback;
}
