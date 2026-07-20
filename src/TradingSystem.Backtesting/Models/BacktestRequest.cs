namespace TradingSystem.Backtesting.Models;

public sealed record BacktestRequest
{
    public required string StrategyName {  get;  init;  }
    public required string Symbol {  get;  init;  }
    public required string Interval {  get;  init;  }
    public DateTime? StartUtc {  get;  init;  }
    public DateTime? EndUtc {  get;  init;  }
    public decimal InitialBalance {  get;  init;  }  =  10_000m;
    public decimal RiskPerTradePercent {  get;  init;  }  =  1m;
    public EntryExecutionMode EntryExecutionMode {  get;  init;  }  =  EntryExecutionMode.NextCandleOpen;
    public IntrabarConflictPolicy ConflictPolicy {  get;  init;  }  =  IntrabarConflictPolicy.WorstCase;
    public decimal SlippageBasisPoints {  get;  init;  }  =  1m;
    public IReadOnlyDictionary<string,  string> StrategyParameters {  get;  init;  }
         =  new Dictionary<string,  string>(StringComparer.OrdinalIgnoreCase);
}
