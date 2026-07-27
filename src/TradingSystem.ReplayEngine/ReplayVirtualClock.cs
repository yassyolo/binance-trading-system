namespace TradingSystem.ReplayEngine;

public sealed class ReplayVirtualClock
{
    public DateTime UtcNow { get; private set; } = DateTime.UnixEpoch;

    public void AdvanceTo(DateTime value)
    {
        var normalized = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };

        if (normalized > UtcNow)
            UtcNow = normalized;
    }
}
