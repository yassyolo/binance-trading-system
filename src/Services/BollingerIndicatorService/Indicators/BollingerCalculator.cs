namespace BollingerIndicatorService.Indicators;

public sealed record BollingerDefinition(
    string Name,
    int Length,
    string Source,
    decimal Mult,
    string MaType);

public sealed record BollingerResult(
    decimal Basis,
    decimal Upper,
    decimal Lower);

public static class BollingerCalculator
{
    public static BollingerResult? Calculate(
        IReadOnlyList<decimal> values,
        int length,
        decimal mult)
    {
        if (values.Count < length)
            return null;

        var window = values.TakeLast(length).ToList();

        var basis = window.Average();

        var variance = window
            .Select(x => (x - basis) * (x - basis))
            .Average();

        var stdDev = (decimal)Math.Sqrt((double)variance);

        var deviation = mult * stdDev;

        return new BollingerResult(
            Basis: basis,
            Upper: basis + deviation,
            Lower: basis - deviation);
    }
}