namespace AlligatorIndicatorService.Indicators;

public sealed class Smma
{
    private readonly int _length;
    private decimal _sum;
    private decimal? _value;
    private int _count;

    public Smma(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        _length = length;
    }

    public decimal Update(decimal source)
    {
        if (_count < _length)
        {
            _sum += source;
            _count++;

            _value = _sum / _count;
            return _value.Value;
        }

        _value = ((_value ?? source) * (_length - 1) + source) / _length;
        return _value.Value;
    }
}
