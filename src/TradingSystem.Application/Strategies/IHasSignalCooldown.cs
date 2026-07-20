namespace TradingSystem.Application.Strategies;

public interface IHasSignalCooldown
{
    TimeSpan SignalCooldown {  get;  }
}
