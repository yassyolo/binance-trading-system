using TradingSystem.Application.Time;

namespace TradingSystem.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow  =>  DateTime.UtcNow;
}
