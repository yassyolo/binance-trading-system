namespace AlligatorIndicatorService.Indicators;

public sealed class Smma
{
    private readonly int _length;
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
        if (_value is null)
        {
            _value = source;
            _count = 1;
            return _value.Value;
        }

        if (_count < _length)
        {
            _value = (_value.Value * _count + source) / (_count + 1);
            _count++;
        }
        else
        {
            _value = (_value.Value * (_length - 1) + source) / _length;
        }

        return _value.Value;
    }
}