using System.Text.Json;
using TradingSystem.Analytics.Contracts;
using TradingSystem.Analytics.Models;
using TradingSystem.Analytics.Models.Enums;
using TradingSystem.Backtesting.Bots.Bot8012;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Signals;
using TradingSystem.Dashboard.Contracts;
using TradingSystem.Dashboard.Contracts.Models.Optimization;
using TradingSystem.JobOrchestration.Contracts;
using TradingSystem.Optimization.Configuration;
using TradingSystem.Optimization.Engine;
using TradingSystem.Optimization.Mapping;
using TradingSystem.Optimization.Models;
namespace TradingSystem.Jobs.Worker.Execution;
public sealed class OptimizationExecutionService(
    IHistoricalMarketDataStore market, 
    IHistoricalSignalStore signalStore, 
    Bot8012BacktestEngine engine, 
    ParameterTuningEngine tuning, 
    WalkForwardOptimizationEngine walkForward, 
    IPerformanceAnalyticsStore analytics)
{
    public async Task<Guid> ExecuteAsync(OptimizationRequest request, string interval, CancellationToken ct)
    {
        if (!request.BotName.Equals("BOT8012", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Dashboard range optimization currently supports BOT8012. Other bots keep their CLI parameter spaces until explicit range binders are added.");

        if (await market.HasGapsAsync(request.Symbol, interval, request.FromUtc, request.ToUtc, ct))
            throw new InvalidOperationException("Historical data contains unresolved candle gaps for the requested period."); 
        
        var candles = await market.LoadCandlesAsync(request.Symbol, interval, request.FromUtc, request.ToUtc, ct); 
        if (candles.Count < 2) 
            throw new InvalidOperationException("Historical candles are missing.");

        var signals = request.SignalSource.Equals("Internal", StringComparison.OrdinalIgnoreCase)
            ? new EmaCrossDemoSignalSource().Generate(candles)
            : await signalStore.LoadAsync(request.BotName, request.Symbol, request.FromUtc, request.ToUtc, ct);

        if (signals.Count == 0)
            throw new InvalidOperationException("No historical signals were found.");

        var runId = Guid.NewGuid();
        var run = new PerformanceRun
        {
            RunId = runId,
            RunType = request.WalkForward ? PerformanceRunType.WalkForward : PerformanceRunType.Optimization,
            BotName = request.BotName,
            StrategyVersion = "1.0.0",
            Symbol = request.Symbol,
            Interval = interval,
            StartedAtUtc = DateTime.UtcNow,
            Status = PerformanceRunStatus.Running,
            ParametersJson = JsonSerializer.Serialize(request)
        };

        await analytics.CreateRunAsync(run, ct);
       
        try
        {
            var candidates = BuildCandidates(request).ToArray();
            if (candidates.Length == 0)
                throw new InvalidOperationException("The parameter ranges produced no candidates.");

            if (request.WalkForward)
            {
                var wf = await walkForward.RunAsync(request.BotName, candles, signals, candidates, (o, c, s, _)
                    => Task.FromResult(engine.Run(c, s, o)), 
                    x => x.Metrics, 
                    new WalkForwardOptions 
                    { 
                        TrainingBars = request.TrainBars ?? 10000, 
                        TestingBars = request.TestBars ?? 2000, 
                        StepBars = request.StepBars ?? 2000 
                    }, 
                    new OptimizationScoreWeights(), 
                    ct);
                
                var windows = wf.Windows.Select(x => new WalkForwardWindow 
                { 
                    WindowId = Guid.NewGuid(), 
                    RunId = runId, 
                    WindowNumber = x.WindowNumber, 
                    TrainFromUtc = x.TrainFromUtc, 
                    TrainToUtc = x.TrainToUtc, 
                    TestFromUtc = x.TestFromUtc, 
                    TestToUtc = x.TestToUtc, 
                    SelectedParametersJson = JsonSerializer.Serialize(x.SelectedOptions), 
                    InSampleScore = x.InSampleScore, 
                    OutOfSampleScore = x.OutOfSampleScore, 
                    InSampleMetrics = BacktestPerformanceMapper.ToMetrics(x.InSampleMetrics), 
                    OutOfSampleMetrics = BacktestPerformanceMapper.ToMetrics(x.OutOfSampleMetrics) 
                }).ToArray(); 
                
                await analytics.SaveWalkForwardWindowsAsync(windows, ct);
            }
            else
            {
                var rows = await tuning.RunAsync(candidates, (o, _) 
                    => Task.FromResult(engine.Run(candles, signals, o)), 
                    x => x.Metrics, 
                    new OptimizationScoreWeights(), 
                    request.TopResults, 
                    ct); 
                
                var trials = rows.Select((x, i) => new OptimizationTrial 
                { 
                    TrialId = Guid.NewGuid(), 
                    OptimizationRunId = runId, 
                    Sequence = x.Sequence, 
                    ParametersJson = JsonSerializer.Serialize(x.Options),
                    Score = x.Score,
                    Metrics = BacktestPerformanceMapper.ToMetrics(x.Metrics),
                    Selected = i == 0 
                }).ToArray();
                
                await analytics.SaveOptimizationTrialsAsync(trials, ct);
            }
            
            await analytics.CompleteRunAsync(runId, PerformanceRunStatus.Completed, DateTime.UtcNow, null, ct);
            
            return runId;
        }
        catch (Exception ex) 
        { 
            await analytics.CompleteRunAsync(runId, PerformanceRunStatus.Failed, DateTime.UtcNow, ex.Message, ct); 
            
            throw; 
        }
    }
    
    private static IEnumerable<Bot8012BacktestOptions> BuildCandidates(OptimizationRequest request)
    {
        var values = request.Ranges.ToDictionary(
            x => x.Name, 
            x => Range(x).ToArray(), 
            StringComparer.OrdinalIgnoreCase);
        
        decimal[] V(string name, decimal fallback) 
            => values.TryGetValue(name, out var x) 
            ? x 
            : [fallback];
        
        foreach (var profit in V("ProfitDistance", 200)) 
            foreach (var gap in V("PriceDistance", 400)) 
                foreach (var cooldown in V("CooldownSeconds", 180)) 
                    foreach (var limit in V("OrderSideLimit", 2)) 
                        yield return new Bot8012BacktestOptions 
                        { 
                            Symbol = request.Symbol, 
                            InitialBalance = request.InitialBalance, 
                            ProfitDistance = profit,
                            PriceDistance = gap, 
                            CooldownSeconds = (int)cooldown,
                            OrderSideLimit = (int)limit 
                        };
    }
    
    private static IEnumerable<decimal> Range(OptimizationRangeDto r) 
    { 
        if (r.Step <= 0 || r.To < r.From) 
            throw new ArgumentException($"Invalid range {r.Name}."); 
        
        for (var value = r.From; value <= r.To; value += r.Step) 
            yield return value; 
    }
}
