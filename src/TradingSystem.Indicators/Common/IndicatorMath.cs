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

public sealed class SmoothedMovingAverage
{
    private readonly int _length; 
    private decimal _sum; 
    private decimal? _value; 
    private int _count;
    
    public SmoothedMovingAverage(int length)
    { 
        if (length <= 0) 
            throw new ArgumentOutOfRangeException(nameof(length));
        
        _length = length; 
    }
    
    public decimal Update(decimal value)
    { 
        if(_count < _length)
        {
            _sum += value;
            _count++;
            
            return (_value = _sum / _count).Value;
        } 
        
        return (_value = ((_value ?? value) * (_length-1) + value) / _length).Value; 
    }
}
