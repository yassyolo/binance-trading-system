namespace TradingSystem.Application.Strategies.Contracts;

public interface IHasSignalCooldown
{
    TimeSpan SignalCooldown { get; }
}
