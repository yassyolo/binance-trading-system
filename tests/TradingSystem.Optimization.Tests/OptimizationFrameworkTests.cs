using TradingSystem.Backtesting.Bots.Models;
using TradingSystem.Backtesting.Bots.Models.Enums;
using TradingSystem.Domain.MarketData;
using TradingSystem.Optimization.Configuration;
using TradingSystem.Optimization.Engine;
using TradingSystem.Optimization.Mapping;
using TradingSystem.Optimization.Models;
using TradingSystem.Optimization.Scoring;
using Xunit;

namespace TradingSystem.Optimization.Tests;

public sealed class OptimizationFrameworkTests
{
    [Fact]
    public async Task ParameterTuning_ShouldOrderByScoreDescending()
    {
        var sut = new ParameterTuningEngine(new PerformanceScoreCalculator());
        var candidates = new[] { 1, 2, 3 };

        var result = await sut.RunAsync(
            candidates,
            (candidate, _) => Task.FromResult(Metrics(returnPercent: candidate)),
            x => x,
            new OptimizationScoreWeights
            {
                ReturnWeight = 1m,
                ProfitFactorWeight = 0m,
                DrawdownPenalty = 0m,
                LowActivityPenalty = 0m
            },
            top: 3);

        Assert.Equal([3, 2, 1], result.Select(x => x.Options).ToArray());
    }

    [Fact]
    public async Task ParameterTuning_WhenScoresTie_ShouldPreferHigherNetProfit()
    {
        var sut = new ParameterTuningEngine(new PerformanceScoreCalculator());
        var candidates = new[] { 1, 2 };

        var result = await sut.RunAsync(
            candidates,
            (candidate, _) => Task.FromResult(
                Metrics(returnPercent: 10m, netProfit: candidate * 100m)),
            x => x,
            new OptimizationScoreWeights
            {
                ReturnWeight = 1m,
                ProfitFactorWeight = 0m,
                DrawdownPenalty = 0m,
                LowActivityPenalty = 0m
            },
            top: 2);

        Assert.Equal([2, 1], result.Select(x => x.Options).ToArray());
    }

    [Fact]
    public async Task ParameterTuning_TopBelowOne_ShouldStillReturnOneCandidate()
    {
        var sut = new ParameterTuningEngine(new PerformanceScoreCalculator());

        var result = await sut.RunAsync(
            new[] { 1, 2 },
            (candidate, _) => Task.FromResult(Metrics(returnPercent: candidate)),
            x => x,
            new OptimizationScoreWeights
            {
                ReturnWeight = 1m,
                ProfitFactorWeight = 0m,
                DrawdownPenalty = 0m,
                LowActivityPenalty = 0m
            },
            top: 0);

        Assert.Single(result);
        Assert.Equal(2, result[0].Options);
    }

    [Fact]
    public async Task ParameterTuning_ShouldAssignSequenceInEvaluationOrder()
    {
        var sut = new ParameterTuningEngine(new PerformanceScoreCalculator());

        var result = await sut.RunAsync(
            new[] { "a", "b", "c" },
            (candidate, _) => Task.FromResult(Metrics(returnPercent: 1m)),
            x => x,
            new OptimizationScoreWeights
            {
                ReturnWeight = 0m,
                ProfitFactorWeight = 0m,
                DrawdownPenalty = 0m,
                LowActivityPenalty = 0m
            },
            top: 3);

        Assert.Equal([1, 2, 3], result.OrderBy(x => x.Sequence).Select(x => x.Sequence).ToArray());
    }

    [Fact]
    public void PerformanceScore_ShouldCapProfitFactorContribution()
    {
        var sut = new PerformanceScoreCalculator();
        var weights = new OptimizationScoreWeights
        {
            ReturnWeight = 0m,
            ProfitFactorWeight = 4m,
            DrawdownPenalty = 0m,
            LowActivityPenalty = 0m,
            MaximumProfitFactorContribution = 5m
        };

        var capped = sut.Calculate(Metrics(profitFactor: 5m), weights);
        var extreme = sut.Calculate(Metrics(profitFactor: decimal.MaxValue), weights);

        Assert.Equal(capped, extreme);
    }

    [Fact]
    public void PerformanceScore_ShouldApplyLowActivityPenalty()
    {
        var sut = new PerformanceScoreCalculator();
        var weights = new OptimizationScoreWeights
        {
            ReturnWeight = 0m,
            ProfitFactorWeight = 0m,
            DrawdownPenalty = 0m,
            LowActivityPenalty = 2m,
            MinimumClosedPositions = 10
        };

        var lowActivity = sut.Calculate(Metrics(closedPositions: 2), weights);
        var sufficientActivity = sut.Calculate(Metrics(closedPositions: 10), weights);

        Assert.Equal(-16m, lowActivity);
        Assert.Equal(0m, sufficientActivity);
    }

    [Fact]
    public async Task WalkForward_AnchoredTraining_ShouldGrowFromBeginning()
    {
        var candles = Candles(8);
        var calls = new List<(int FirstMinute, int CandleCount)>();
        var sut = CreateWalkForward();

        await sut.RunAsync(
            "BOT8012",
            candles,
            [],
            new[] { 1 },
            (candidate, slice, _, _) =>
            {
                calls.Add((slice[0].OpenTimeUtc.Minute, slice.Count));
                return Task.FromResult(Metrics(returnPercent: candidate));
            },
            x => x,
            new WalkForwardOptions
            {
                TrainingBars = 4,
                TestingBars = 2,
                StepBars = 2,
                AnchoredTraining = true
            },
            ZeroRiskWeights());

        Assert.Equal((0, 4), calls[0]);
        Assert.Equal((4, 2), calls[1]);
        Assert.Equal((0, 6), calls[2]);
        Assert.Equal((6, 2), calls[3]);
    }

    [Theory]
    [InlineData(1, 1, 1, 1)]
    [InlineData(2, 0, 1, 1)]
    [InlineData(2, 1, 0, 1)]
    [InlineData(2, 1, 1, 0)]
    public async Task WalkForward_InvalidOptions_ShouldThrow(
        int training,
        int testing,
        int step,
        int top)
    {
        var sut = CreateWalkForward();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.RunAsync(
            "BOT8012",
            Candles(8),
            [],
            new[] { 1 },
            (candidate, _, _, _) => Task.FromResult(Metrics(returnPercent: candidate)),
            x => x,
            new WalkForwardOptions
            {
                TrainingBars = training,
                TestingBars = testing,
                StepBars = step,
                TopCandidatesPerWindow = top
            },
            ZeroRiskWeights()));
    }

    [Fact]
    public async Task WalkForward_EmptyCandidates_ShouldThrow()
    {
        var sut = CreateWalkForward();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync<int, BotBacktestMetrics>(
            "BOT8012",
            Candles(8),
            [],
            [],
            (candidate, _, _, _) => Task.FromResult(Metrics(returnPercent: candidate)),
            x => x,
            new WalkForwardOptions
            {
                TrainingBars = 4,
                TestingBars = 2,
                StepBars = 2
            },
            ZeroRiskWeights()));
    }

    [Fact]
    public void BacktestPerformanceMapper_ShouldMapRunSnapshotAndTrades()
    {
        var candles = Candles(2);
        var position = new BotPositionResult
        {
            PositionId = "p1",
            Side = TradeSide.Long,
            EntryTimeUtc = candles[0].OpenTimeUtc,
            EntryPrice = 100m,
            ExitTimeUtc = candles[1].CloseTimeUtc,
            ExitPrice = 110m,
            Quantity = 1m,
            GrossPnl = 10m,
            Fees = 1m,
            NetPnl = 9m,
            ExitReason = "TAKE_PROFIT",
            PartialTakeProfitReached = true
        };
        var result = new BotBacktestResult<TestOptions>
        {
            RunId = "source-run",
            BotName = "BOT8012",
            Options = new TestOptions(200m),
            StartedAtUtc = candles[0].OpenTimeUtc,
            CompletedAtUtc = candles[1].CloseTimeUtc,
            Metrics = Metrics(
                returnPercent: 9m,
                netProfit: 9m,
                closedPositions: 1) with
            {
                InitialBalance = 100m,
                FinalBalance = 109m,
                WinningPositions = 1,
                TotalFees = 1m
            },
            Positions = [position],
            Executions = [],
            Decisions = [],
            EquityCurve = [],
            Candles = candles,
            Signals = []
        };

        var mapped = BacktestPerformanceMapper.Map(
            result,
            strategyVersion: "2.0.0",
            interval: "5m",
            score: 42m);

        Assert.Equal(mapped.Run.RunId, mapped.Snapshot.RunId);
        Assert.Equal("BOT8012", mapped.Run.BotName);
        Assert.Equal("2.0.0", mapped.Run.StrategyVersion);
        Assert.Equal("5m", mapped.Run.Interval);
        Assert.Equal(42m, mapped.Snapshot.Score);

        var trade = Assert.Single(mapped.Trades);
        Assert.Equal("p1", trade.PositionId);
        Assert.Equal(9m, trade.NetPnl);
        Assert.True(trade.PartialTakeProfitReached);
    }

    private static WalkForwardOptimizationEngine CreateWalkForward()
    {
        var score = new PerformanceScoreCalculator();
        return new WalkForwardOptimizationEngine(
            new ParameterTuningEngine(score),
            score);
    }

    private static OptimizationScoreWeights ZeroRiskWeights()
        => new()
        {
            ReturnWeight = 1m,
            ProfitFactorWeight = 0m,
            DrawdownPenalty = 0m,
            LowActivityPenalty = 0m
        };

    private static BotBacktestMetrics Metrics(
        decimal returnPercent = 0m,
        decimal netProfit = 0m,
        decimal profitFactor = 0m,
        int closedPositions = 20)
        => new()
        {
            ReturnPercent = returnPercent,
            NetProfit = netProfit,
            ProfitFactor = profitFactor,
            ClosedPositions = closedPositions
        };

    private static MarketCandle[] Candles(int count)
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return Enumerable.Range(0, count)
            .Select(i =>
            {
                var open = start.AddMinutes(i);
                return new MarketCandle(
                    "BTCUSDC",
                    "1m",
                    open,
                    open.AddMinutes(1).AddMilliseconds(-1),
                    100m,
                    101m,
                    99m,
                    100m,
                    1m,
                    true);
            })
            .ToArray();
    }

    private sealed record TestOptions(decimal ProfitDistance);
}
