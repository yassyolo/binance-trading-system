namespace TradingSystem.Indicators.Common;

public static class IndicatorMath
{
    public static decimal Average(IEnumerable<decimal> values)  
        => values.Average();
    
    public static decimal StandardDeviation(IReadOnlyCollection<decimal> values)
    {
        var average = values.Average();
        var variance = values.Average(x => (x - average) * (x - average));
       
        return (decimal)Math.Sqrt((double)variance);
    }
}