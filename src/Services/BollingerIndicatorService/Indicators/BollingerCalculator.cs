namespace BollingerIndicatorService.Indicators;

public sealed record BollingerResult(
    decimal Basis,
    decimal Upper,
    decimal Lower);

public static class BollingerCalculator
{
    public static BollingerResult? Calculate(IEnumerable<decimal> values, int length, decimal mult)
    {
        var window = values.TakeLast(length).ToArray();

        if (window.Length < length)
            return null;

        var basis = window.Average();

        var variance = window.Select(x => (x - basis) * (x - basis)).Average();

        var stdDev = (decimal)Math.Sqrt((double)variance);
        var deviation = mult * stdDev;

        return new BollingerResult(
            basis,
            basis + deviation,
            basis - deviation);
    }
}