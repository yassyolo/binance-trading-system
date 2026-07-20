namespace TradingSystem.Backtesting.Models;

public sealed class BacktestPosition
{
    public required long Id {  get;  init;  }
    public required string Symbol {  get;  init;  }
    public required TradeSide Side {  get;  init;  }
    public required decimal EntryPrice {  get;  init;  }
    public required decimal Quantity {  get;  init;  }
    public required DateTime EntryTimeUtc {  get;  init;  }
    public required decimal EntryFee {  get;  init;  }
    public decimal? StopLoss {  get;  set; }
    public decimal? TakeProfit {  get;  set; }
    public decimal InitialRiskAmount {  get;  init;  }
}
