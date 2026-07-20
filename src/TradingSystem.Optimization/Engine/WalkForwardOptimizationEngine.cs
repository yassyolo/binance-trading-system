using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Domain.MarketData;
using TradingSystem.Optimization.Models;
using TradingSystem.Optimization.Scoring;

namespace TradingSystem.Optimization.Engine;

public sealed class WalkForwardOptimizationEngine(
    ParameterTuningEngine tuning, 
    PerformanceScoreCalculator scoreCalculator)
{
    public async Task<WalkForwardResult<TOptions>> RunAsync<TOptions,  TResult>(
        string botName, 
        IReadOnlyList<MarketCandle> candles, 
        IReadOnlyList<HistoricalBotSignal> signals, 
        IEnumerable<TOptions> candidates, 
        Func<TOptions,  IReadOnlyList<MarketCandle>,  IReadOnlyList<HistoricalBotSignal>,  CancellationToken,  Task<TResult>> run, 
        Func<TResult,  BotBacktestMetrics> metricsSelector, 
        WalkForwardOptions options, 
        OptimizationScoreWeights weights, 
        CancellationToken cancellationToken  =  default)
    {
        Validate(options,  candles.Count);
        var started  =  DateTime.UtcNow;
        var windows  =  new List<WalkForwardWindowResult<TOptions>>();
        var window  =  0;

        for (var testStart  =  options.TrainingBars; testStart + options.TestingBars <= candles.Count; testStart += options.StepBars)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var trainStart  =  options.AnchoredTraining ? 0 : testStart - options.TrainingBars;
            var training  =  candles.Skip(trainStart).Take(testStart - trainStart).ToArray();
            var testing  =  candles.Skip(testStart).Take(options.TestingBars).ToArray();
            var trainSignals  =  SliceSignals(signals,  training);
            var testSignals  =  SliceSignals(signals,  testing);

            var best  =  (await tuning.RunAsync(
                candidates, 
                (candidate,  ct)  =>  run(candidate,  training,  trainSignals,  ct), 
                metricsSelector, 
                weights, 
                options.TopCandidatesPerWindow, 
                cancellationToken)).First();

            var outOfSample  =  await run(best.Options,  testing,  testSignals,  cancellationToken);
            var outMetrics  =  metricsSelector(outOfSample);
            windows.Add(new WalkForwardWindowResult<TOptions>
            {
                WindowNumber  =  ++window, 
                TrainFromUtc  =  training.First().OpenTimeUtc, 
                TrainToUtc  =  training.Last().CloseTimeUtc, 
                TestFromUtc  =  testing.First().OpenTimeUtc, 
                TestToUtc  =  testing.Last().CloseTimeUtc, 
                SelectedOptions  =  best.Options, 
                InSampleScore  =  best.Score, 
                OutOfSampleScore  =  scoreCalculator.Calculate(outMetrics,  weights), 
                InSampleMetrics  =  best.Metrics, 
                OutOfSampleMetrics  =  outMetrics
            });
        }

        return new WalkForwardResult<TOptions>
        {
            BotName  =  botName, 
            StartedAtUtc  =  started, 
            CompletedAtUtc  =  DateTime.UtcNow, 
            Windows  =  windows
        };
    }

    private static IReadOnlyList<HistoricalBotSignal> SliceSignals(IReadOnlyList<HistoricalBotSignal> signals,  IReadOnlyList<MarketCandle> candles)
    {
        var from  =  candles.First().OpenTimeUtc;
        var to  =  candles.Last().CloseTimeUtc;
        return signals.Where(x  =>  x.TimeUtc >= from  &&  x.TimeUtc <= to).ToArray();
    }

    private static void Validate(WalkForwardOptions options,  int candleCount)
    {
        if (options.TrainingBars < 2) throw new ArgumentOutOfRangeException(nameof(options.TrainingBars));
        if (options.TestingBars < 1) throw new ArgumentOutOfRangeException(nameof(options.TestingBars));
        if (options.StepBars < 1) throw new ArgumentOutOfRangeException(nameof(options.StepBars));
        if (candleCount < options.TrainingBars + options.TestingBars)
            throw new InvalidOperationException("Not enough candles for one walk-forward window.");
    }
}
