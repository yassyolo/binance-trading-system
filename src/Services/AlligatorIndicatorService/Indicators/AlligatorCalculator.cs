namespace AlligatorIndicatorService.Indicators;

public sealed class AlligatorCalculator
{
    private readonly Smma _jaw;
    private readonly Smma _teeth;
    private readonly Smma _lips;

    public AlligatorCalculator(int jawLength, int teethLength, int lipsLength)
    {
        _jaw = new Smma(jawLength);
        _teeth = new Smma(teethLength);
        _lips = new Smma(lipsLength);
    }

    public AlligatorValues Update(decimal high, decimal low)
    {
        var hl2 = (high + low) / 2;

        return new AlligatorValues(
            Jaw: _jaw.Update(hl2),
            Teeth: _teeth.Update(hl2),
            Lips: _lips.Update(hl2));
    }
}

public sealed record AlligatorValues(
    decimal Jaw,
    decimal Teeth,
    decimal Lips);