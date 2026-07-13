namespace StrategyService.Market;

public sealed class Bot8011TrailingPriceCache
{
    private readonly object _sync = new();
    private decimal? _price;
    private DateTime? _receivedAtUtc;

    public void Set(decimal price)
    {
        lock (_sync)
        {
            _price = price;
            _receivedAtUtc = DateTime.UtcNow;
        }
    }

    public bool TryGetFresh(TimeSpan maxAge, out decimal price)
    {
        lock (_sync)
        {
            price = _price ?? 0;

            return _price is > 0
                && _receivedAtUtc.HasValue
                && DateTime.UtcNow - _receivedAtUtc.Value <= maxAge;
        }
    }
}
