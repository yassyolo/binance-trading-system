namespace StrategyService.Market;

public sealed class Bot8015TrailingPriceCache
{
    private readonly object _sync = new();
    private decimal? _latestClose;
    private DateTime? _latestCloseAtUtc;

    public void Update(decimal close, DateTime closeAtUtc)
    {
        if (close <= 0)
            return;

        lock (_sync)
        {
            _latestClose = close;
            _latestCloseAtUtc = closeAtUtc;
        }
    }

    public bool TryGetFresh(
        TimeSpan maxAge,
        out decimal price)
    {
        lock (_sync)
        {
            if (_latestClose is > 0
                && _latestCloseAtUtc.HasValue
                && DateTime.UtcNow - _latestCloseAtUtc.Value <= maxAge)
            {
                price = _latestClose.Value;
                return true;
            }
        }

        price = 0;
        return false;
    }
}
