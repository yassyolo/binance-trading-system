using TradingSystem.Contracts.Indicators;
using TradingSystem.Domain.MarketData;
namespace TradingSystem.Indicators.Abstractions;
public interface IIndicatorProcessor
{
    string Name {  get;  }
    IReadOnlyCollection<string> Symbols {  get;  }
    IReadOnlyCollection<string> Intervals {  get;  }
    int RequiredHistory {  get;  }
    void Initialize(string symbol,  string interval,  IReadOnlyList<MarketCandle> candles);
    IndicatorSnapshotMessage? Process(MarketCandle candle,  DateTimeOffset publishedAt);
}
