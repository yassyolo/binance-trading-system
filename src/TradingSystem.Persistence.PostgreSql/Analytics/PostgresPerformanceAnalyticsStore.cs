using System.Text.Json;
using Dapper;
using TradingSystem.Analytics.Abstractions;
using TradingSystem.Analytics.Models;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Persistence.PostgreSql.Analytics;

public sealed class PostgresPerformanceAnalyticsStore(ITradingDbConnectionFactory connections) : IPerformanceAnalyticsStore
{
    private static readonly JsonSerializerOptions JsonOptions  =  new(JsonSerializerDefaults.Web);

    public async Task CreateRunAsync(PerformanceRun run, CancellationToken ct = default)
    {
        const string sql  =  """
            INSERT INTO trading.performance_runs
            (run_id,  run_type,  bot_name,  strategy_version,  symbol,  interval,  started_at_utc,  completed_at_utc,  status,  parameters,  parent_run_id,  notes)
            VALUES (@RunId,  @RunType,  @BotName,  @StrategyVersion,  @Symbol,  @Interval,  @StartedAtUtc,  @CompletedAtUtc,  @Status,  CAST(@ParametersJson AS jsonb),  @ParentRunId,  @Notes)
            ON CONFLICT (run_id) DO UPDATE SET status  =  EXCLUDED.status,  completed_at_utc  =  EXCLUDED.completed_at_utc,  notes  =  EXCLUDED.notes;
            """;
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql,  new
        {
            run.RunId,  RunType  =  run.RunType.ToString(),  run.BotName,  run.StrategyVersion,  run.Symbol,  run.Interval, 
            run.StartedAtUtc,  run.CompletedAtUtc,  Status  =  run.Status.ToString(),  run.ParametersJson,  run.ParentRunId,  run.Notes
        },  cancellationToken: ct));
    }

    public async Task CompleteRunAsync(Guid runId, PerformanceRunStatus status, DateTime completedAtUtc, string? notes, CancellationToken ct = default)
    {
        const string sql  =  "UPDATE trading.performance_runs SET status = @Status,  completed_at_utc = @CompletedAtUtc,  notes = COALESCE(@Notes,  notes) WHERE run_id = @RunId";
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql,  new { RunId  =  runId,  Status  =  status.ToString(),  CompletedAtUtc  =  completedAtUtc,  Notes  =  notes },  cancellationToken: ct));
    }

    public async Task SaveSnapshotAsync(PerformanceSnapshot s, CancellationToken ct = default)
    {
        const string sql  =  """
        INSERT INTO trading.performance_snapshots
        (run_id,  bot_name,  symbol,  period_from_utc,  period_to_utc,  signals,  opened_positions,  blocked_signals,  closed_positions, 
         winning_positions,  losing_positions,  initial_balance,  final_balance,  net_profit,  return_percent,  win_rate_percent, 
         profit_factor,  maximum_drawdown_amount,  maximum_drawdown_percent,  total_fees,  expectancy,  score)
        VALUES (@RunId, @BotName, @Symbol, @PeriodFromUtc, @PeriodToUtc, @Signals, @OpenedPositions, @BlockedSignals, @ClosedPositions, 
         @WinningPositions, @LosingPositions, @InitialBalance, @FinalBalance, @NetProfit, @ReturnPercent, @WinRatePercent, 
         @ProfitFactor, @MaximumDrawdownAmount, @MaximumDrawdownPercent, @TotalFees, @Expectancy, @Score)
        ON CONFLICT (run_id) DO UPDATE SET final_balance = EXCLUDED.final_balance,  net_profit = EXCLUDED.net_profit, 
         return_percent = EXCLUDED.return_percent,  maximum_drawdown_percent = EXCLUDED.maximum_drawdown_percent,  score = EXCLUDED.score;
        """;
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql,  new
        {
            s.RunId,  s.BotName,  s.Symbol,  s.PeriodFromUtc,  s.PeriodToUtc, 
            s.Metrics.Signals,  s.Metrics.OpenedPositions,  s.Metrics.BlockedSignals,  s.Metrics.ClosedPositions, 
            s.Metrics.WinningPositions,  s.Metrics.LosingPositions,  s.Metrics.InitialBalance,  s.Metrics.FinalBalance, 
            s.Metrics.NetProfit,  s.Metrics.ReturnPercent,  s.Metrics.WinRatePercent,  s.Metrics.ProfitFactor, 
            s.Metrics.MaximumDrawdownAmount,  s.Metrics.MaximumDrawdownPercent,  s.Metrics.TotalFees, 
            s.Metrics.Expectancy,  s.Score
        },  cancellationToken: ct));
    }

    public async Task SaveTradesAsync(Guid runId,  IReadOnlyCollection<PerformanceTrade> trades, CancellationToken ct = default)
    {
        if (trades.Count == 0) return;
        const string sql  =  """
        INSERT INTO trading.performance_trades
        (run_id,  position_id,  side,  entry_time_utc,  entry_price,  exit_time_utc,  exit_price,  quantity,  gross_pnl,  fees,  net_pnl,  exit_reason,  partial_take_profit_reached)
        VALUES (@RunId, @PositionId, @Side, @EntryTimeUtc, @EntryPrice, @ExitTimeUtc, @ExitPrice, @Quantity, @GrossPnl, @Fees, @NetPnl, @ExitReason, @PartialTakeProfitReached)
        ON CONFLICT (run_id,  position_id) DO NOTHING;
        """;
        var rows  =  trades.Select(x  =>  new { RunId  =  runId,  x.PositionId,  Side  =  x.Side.ToString(),  x.EntryTimeUtc,  x.EntryPrice,  x.ExitTimeUtc,  x.ExitPrice,  x.Quantity,  x.GrossPnl,  x.Fees,  x.NetPnl,  x.ExitReason,  x.PartialTakeProfitReached });
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql,  rows,  cancellationToken: ct));
    }

    public async Task SaveOptimizationTrialsAsync(IReadOnlyCollection<OptimizationTrial> trials, CancellationToken ct = default)
    {
        if (trials.Count == 0) return;
        const string sql  =  """
        INSERT INTO trading.optimization_trials
        (trial_id,  optimization_run_id,  sequence,  parameters,  score,  metrics,  selected)
        VALUES (@TrialId, @OptimizationRunId, @Sequence, CAST(@ParametersJson AS jsonb), @Score, CAST(@MetricsJson AS jsonb), @Selected)
        ON CONFLICT (trial_id) DO NOTHING;
        """;
        var rows  =  trials.Select(x  =>  new { x.TrialId,  x.OptimizationRunId,  x.Sequence,  x.ParametersJson,  x.Score,  MetricsJson  =  JsonSerializer.Serialize(x.Metrics,  JsonOptions),  x.Selected });
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, rows, cancellationToken: ct));
    }

    public async Task SaveWalkForwardWindowsAsync(IReadOnlyCollection<WalkForwardWindow> windows, CancellationToken ct = default)
    {
        if (windows.Count == 0) return;
        const string sql  =  """
        INSERT INTO trading.walk_forward_windows
        (window_id,  run_id,  window_number,  train_from_utc,  train_to_utc,  test_from_utc,  test_to_utc, 
         selected_parameters,  in_sample_score,  out_of_sample_score,  in_sample_metrics,  out_of_sample_metrics)
        VALUES (@WindowId, @RunId, @WindowNumber, @TrainFromUtc, @TrainToUtc, @TestFromUtc, @TestToUtc, 
         CAST(@SelectedParametersJson AS jsonb), @InSampleScore, @OutOfSampleScore, CAST(@InSampleMetricsJson AS jsonb), CAST(@OutOfSampleMetricsJson AS jsonb))
        ON CONFLICT (window_id) DO NOTHING;
        """;
        var rows  =  windows.Select(x  =>  new { x.WindowId,  x.RunId,  x.WindowNumber,  x.TrainFromUtc,  x.TrainToUtc,  x.TestFromUtc,  x.TestToUtc,  x.SelectedParametersJson,  x.InSampleScore,  x.OutOfSampleScore,  InSampleMetricsJson  =  JsonSerializer.Serialize(x.InSampleMetrics,  JsonOptions),  OutOfSampleMetricsJson  =  JsonSerializer.Serialize(x.OutOfSampleMetrics,  JsonOptions) });
        await using var connection  =  await connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql,  rows,  cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PerformanceSnapshot>> QuerySnapshotsAsync(PerformanceQuery query, CancellationToken ct = default)
    {
        const string sql  =  """
        SELECT s.* FROM trading.performance_snapshots s
        JOIN trading.performance_runs r ON r.run_id = s.run_id
        WHERE (@BotName IS NULL OR r.bot_name = @BotName) AND (@Symbol IS NULL OR r.symbol = @Symbol)
          AND (@RunType IS NULL OR r.run_type = @RunType) AND (@FromUtc IS NULL OR s.period_to_utc>=@FromUtc)
          AND (@ToUtc IS NULL OR s.period_from_utc<=@ToUtc)
        ORDER BY s.period_to_utc DESC LIMIT @Take;
        """;
        await using var connection  =  await connections.OpenAsync(ct);
        var result  =  await connection.QueryAsync<PerformanceSnapshot>(new CommandDefinition(sql,  new { query.BotName,  query.Symbol,  RunType  =  query.RunType?.ToString(),  query.FromUtc,  query.ToUtc,  Take  =  Math.Clamp(query.Take,  1,  1000) },  cancellationToken: ct));
        return result.AsList();
    }

    public async Task<IReadOnlyList<PerformanceRun>> QueryRunsAsync(PerformanceQuery query, CancellationToken ct = default)
    {
        const string sql  =  """
        SELECT run_id RunId,  run_type RunType,  bot_name BotName,  strategy_version StrategyVersion,  symbol Symbol,  interval Interval, 
        started_at_utc StartedAtUtc,  completed_at_utc CompletedAtUtc,  status Status,  parameters::text ParametersJson,  parent_run_id ParentRunId,  notes Notes
        FROM trading.performance_runs
        WHERE (@BotName IS NULL OR bot_name = @BotName) AND (@Symbol IS NULL OR symbol = @Symbol)
          AND (@RunType IS NULL OR run_type = @RunType) AND (@FromUtc IS NULL OR started_at_utc>=@FromUtc)
          AND (@ToUtc IS NULL OR started_at_utc<=@ToUtc)
        ORDER BY started_at_utc DESC LIMIT @Take;
        """;
        await using var connection  =  await connections.OpenAsync(ct);
        var result  =  await connection.QueryAsync<PerformanceRun>(new CommandDefinition(sql,  new { query.BotName,  query.Symbol,  RunType  =  query.RunType?.ToString(),  query.FromUtc,  query.ToUtc,  Take  =  Math.Clamp(query.Take,  1,  1000) },  cancellationToken: ct));
        return result.AsList();
    }
}
