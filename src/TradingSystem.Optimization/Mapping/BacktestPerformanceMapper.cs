using System.Text.Json;
using TradingSystem.Analytics.Models;
using TradingSystem.Analytics.Models.Enums;
using TradingSystem.Backtesting.Bots.Models;

namespace TradingSystem.Optimization.Mapping;

public static class BacktestPerformanceMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static (PerformanceRun Run, PerformanceSnapshot Snapshot, IReadOnlyList<PerformanceTrade> Trades) Map<TOptions>(
        BotBacktestResult<TOptions> result, 
        string strategyVersion, 
        string interval, 
        decimal score = 0m)
    {
        var runId = Guid.NewGuid();
        var metrics = ToMetrics(result.Metrics);
        
        var run = new PerformanceRun
        {
            RunId = runId, 
            RunType = PerformanceRunType.Backtest, 
            BotName = result.BotName, 
            StrategyVersion = strategyVersion, 
            Symbol = result.Candles.First().Symbol, 
            Interval = interval, 
            StartedAtUtc = result.StartedAtUtc, 
            CompletedAtUtc = result.CompletedAtUtc, 
            Status = PerformanceRunStatus.Completed, 
            ParametersJson = JsonSerializer.Serialize(result.Options,  JsonOptions)
        };
        
        var snapshot = new PerformanceSnapshot
        {
            RunId = runId, 
            BotName = result.BotName, 
            Symbol = result.Candles.First().Symbol, 
            PeriodFromUtc = result.Candles.First().OpenTimeUtc, 
            PeriodToUtc = result.Candles.Last().CloseTimeUtc, 
            Metrics = metrics, 
            Score = score
        };
        
        var trades = result.Positions.Select(x => new PerformanceTrade
        {
            PositionId = x.PositionId, 
            Side = x.Side.ToString(), 
            EntryTimeUtc = x.EntryTimeUtc, 
            EntryPrice = x.EntryPrice, 
            ExitTimeUtc = x.ExitTimeUtc, 
            ExitPrice = x.ExitPrice, 
            Quantity = x.Quantity, 
            GrossPnl = x.GrossPnl, 
            Fees = x.Fees, 
            NetPnl = x.NetPnl, 
            ExitReason = x.ExitReason, 
            PartialTakeProfitReached = x.PartialTakeProfitReached
        }).ToArray();
       
        return (run,  snapshot,  trades);
    }

    public static PerformanceMetricSet ToMetrics(BotBacktestMetrics m) => new()
    {
        Signals = m.Signals, 
        OpenedPositions = m.OpenedPositions, 
        BlockedSignals = m.BlockedSignals, 
        ClosedPositions = m.ClosedPositions, 
        WinningPositions = m.WinningPositions, 
        LosingPositions = m.LosingPositions, 
        InitialBalance = m.InitialBalance, 
        FinalBalance = m.FinalBalance, 
        NetProfit = m.NetProfit, 
        ReturnPercent = m.ReturnPercent, 
        WinRatePercent = m.WinRatePercent, 
        ProfitFactor = m.ProfitFactor, 
        MaximumDrawdownAmount = m.MaximumDrawdownAmount, 
        MaximumDrawdownPercent = m.MaximumDrawdownPercent, 
        TotalFees = m.TotalFees, 
        Expectancy = m.Expectancy
    };
}
