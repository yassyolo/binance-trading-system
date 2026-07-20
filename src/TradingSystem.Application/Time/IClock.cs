namespace TradingSystem.Application.Time;

public interface IClock
{
    DateTime UtcNow {  get;  }
}
