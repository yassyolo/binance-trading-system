using TradingSystem.Backtesting.Bots.Common;

namespace TradingSystem.Optimization.Models;

public sealed record OptimizationScoreWeights
{
    public decimal ReturnWeight {  get;  init;  }  =  1m;
    public decimal ProfitFactorWeight {  get;  init;  }  =  4m;
    public decimal DrawdownPenalty {  get;  init;  }  =  1.5m;
    public decimal LowActivityPenalty {  get;  init;  }  =  2m;
    public int MinimumClosedPositions {  get;  init;  }  =  10;
    public decimal MaximumProfitFactorContribution {  get;  init;  }  =  5m;
}

public sealed record ParameterTrial<TOptions>
{
    public required int Sequence {  get;  init;  }
    public required TOptions Options {  get;  init;  }
    public required BotBacktestMetrics Metrics {  get;  init;  }
    public required decimal Score {  get;  init;  }
}

public sealed record WalkForwardOptions
{
    public int TrainingBars {  get;  init;  }  =  10_000;
    public int TestingBars {  get;  init;  }  =  2_000;
    public int StepBars {  get;  init;  }  =  2_000;
    public bool AnchoredTraining {  get;  init;  }
    public int TopCandidatesPerWindow {  get;  init;  }  =  1;
}

public sealed record WalkForwardWindowResult<TOptions>
{
    public required int WindowNumber {  get;  init;  }
    public required DateTime TrainFromUtc {  get;  init;  }
    public required DateTime TrainToUtc {  get;  init;  }
    public required DateTime TestFromUtc {  get;  init;  }
    public required DateTime TestToUtc {  get;  init;  }
    public required TOptions SelectedOptions {  get;  init;  }
    public required decimal InSampleScore {  get;  init;  }
    public required decimal OutOfSampleScore {  get;  init;  }
    public required BotBacktestMetrics InSampleMetrics {  get;  init;  }
    public required BotBacktestMetrics OutOfSampleMetrics {  get;  init;  }
}

public sealed record WalkForwardResult<TOptions>
{
    public required string BotName {  get;  init;  }
    public required DateTime StartedAtUtc {  get;  init;  }
    public required DateTime CompletedAtUtc {  get;  init;  }
    public required IReadOnlyList<WalkForwardWindowResult<TOptions>> Windows {  get;  init;  }
    public decimal AverageOutOfSampleScore  =>  Windows.Count == 0 ? 0m : Windows.Average(x  =>  x.OutOfSampleScore);
    public decimal TotalOutOfSampleNetProfit  =>  Windows.Sum(x  =>  x.OutOfSampleMetrics.NetProfit);
    public decimal WorstOutOfSampleDrawdownPercent  =>  Windows.Count == 0 ? 0m : Windows.Max(x  =>  x.OutOfSampleMetrics.MaximumDrawdownPercent);
}
