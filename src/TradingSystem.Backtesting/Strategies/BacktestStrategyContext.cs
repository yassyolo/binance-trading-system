using TradingSystem.Domain.MarketData;
using TradingSystem.Backtesting.Models;

namespace TradingSystem.Backtesting.Strategies;

public sealed record BacktestStrategyContext
{
    public required MarketCandle CurrentCandle {  get;  init;  }
    public required MarketCandle? PreviousCandle {  get;  init;  }
    public required IReadOnlyList<MarketCandle> History {  get;  init;  }
    public required BacktestPosition? ActivePosition {  get;  init;  }
    public required decimal Balance {  get;  init;  }
    public required int BarIndex {  get;  init;  }
}
