using System.Text.Json;
using Dapper;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Dashboard.Application.Models;
using TradingSystem.Dashboard.Contracts.Models.Alerts;
using TradingSystem.Dashboard.Contracts.Models.Analytics;
using TradingSystem.Dashboard.Contracts.Models.Audit;
using TradingSystem.Dashboard.Contracts.Models.Backtesting;
using TradingSystem.Dashboard.Contracts.Models.Bots;
using TradingSystem.Dashboard.Contracts.Models.Charts;
using TradingSystem.Dashboard.Contracts.Models.Health;
using TradingSystem.Dashboard.Contracts.Models.Jobs;
using TradingSystem.Dashboard.Contracts.Models.Optimization;
using TradingSystem.Dashboard.Contracts.Models.Positions;
using TradingSystem.Dashboard.Contracts.Models.Signals;
using TradingSystem.Dashboard.Contracts.Models.Trades;
using TradingSystem.Persistence.PostgreSql.Connections;
namespace TradingSystem.Persistence.PostgreSql.Dashboard;

public sealed class PostgresDashboardStore(ITradingDbConnectionFactory factory) : IDashboardQueryStore, IBotConfigurationStore, IBotCommandStore, IDashboardJobStore, IAlertCommandStore
{
    private static int Take(int value) => Math.Clamp(value, 1, 1000);
    public async Task<LiveOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);

        const string botsSql = """
            select
                bot_name as "BotName",
                status as "Status",
                environment as "Environment",
                signal_source as "SignalSource",
                last_signal_side as "LastSignalSide",
                last_signal_at_utc as "LastSignalAtUtc",
                last_decision as "LastDecision",
                last_decision_reason as "LastDecisionReason",
                last_decision_at_utc as "LastDecisionAtUtc",
                open_positions::int as "OpenPositions",
                coalesce(unrealized_pnl, 0)::numeric(28,8) as "UnrealizedPnl",
                coalesce(realized_pnl_today, 0)::numeric(28,8) as "RealizedPnlToday",
                strategy_version as "StrategyVersion",
                last_heartbeat_utc as "LastHeartbeatUtc"
            from trading_dashboard.v_live_bot_overview
            order by bot_name;
            """;

        var bots = (await c.QueryAsync<BotOverviewDto>(new CommandDefinition(botsSql, cancellationToken: ct))).AsList();

        var health = await GetHealthWithConnection(c, ct);

        const string totalsSql = """
            select
                coalesce(sum(realized_pnl_today), 0)::numeric(28,8) as "Realized",
                coalesce(sum(unrealized_pnl), 0)::numeric(28,8) as "Unrealized",
                coalesce(sum(open_positions), 0)::int as "Positions",
                (
                    select count(*)::int
                    from trading_dashboard.alerts
                    where not acknowledged
                      and not resolved
                      and severity = 'Critical'
                ) as "Alerts"
            from trading_dashboard.v_live_bot_overview;
            """;

        var totals =
            await c.QuerySingleAsync<OverviewTotalsRow>(
                new CommandDefinition(
                    totalsSql,
                    cancellationToken: ct));

        return new LiveOverviewDto(
            DateTime.UtcNow,
            bots,
            health,
            totals.Realized,
            totals.Unrealized,
            totals.Positions,
            totals.Alerts);
    }

    public async Task<IReadOnlyCollection<SignalRowDto>> GetSignalsAsync(DashboardQuery q, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);

        return (await c.QueryAsync<SignalRowDto>(new CommandDefinition("select row_number() over(order by s.signal_time_utc desc) Id, s.signal_id SignalId, s.signal_time_utc TimeUtc, s.bot_name BotName, s.symbol Symbol, s.source Source, s.side Side, s.reference_price Price, d.decision Decision, d.reason BlockReason, s.strategy_version StrategyVersion, s.environment Environment from trading_history.signals s left join lateral(select decision, reason from trading_history.strategy_decisions d where d.signal_id = s.signal_id order by decided_at_utc desc limit 1)d on true where (@Bot is null or s.bot_name = @Bot) and (@Symbol is null or s.symbol = @Symbol) and (cast(@FromUtc as timestamptz) is null or s.signal_time_utc >= cast(@FromUtc as timestamptz)) and (cast(@ToUtc as timestamptz) is null or s.signal_time_utc < cast(@ToUtc as timestamptz)) order by s.signal_time_utc desc offset @Skip limit @Take", new { Bot = q.BotName, Symbol = q.Symbol, q.FromUtc, q.ToUtc, q.Skip, Take = Take(q.Take) }, cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyCollection<PositionRowDto>> GetPositionsAsync(DashboardQuery q, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);

        return (await c.QueryAsync<PositionRowDto>(new CommandDefinition("select parameters.position_id PositionId, parameters.bot_name BotName, parameters.symbol Symbol, parameters.side Side, parameters.status Status, parameters.quantity Quantity, parameters.entry_price EntryPrice, parameters.take_profit_price TakeProfitPrice, null::numeric CurrentPrice, 0::numeric UnrealizedPnl, parameters.realized_pnl RealizedPnl, parameters.opened_at_utc OpenedAtUtc, parameters.closed_at_utc ClosedAtUtc, parameters.strategy_version StrategyVersion, parameters.environment Environment from trading_history.positions parameters where (@Bot is null or parameters.bot_name = @Bot) and (@Symbol is null or parameters.symbol = @Symbol) and (@Status is null or parameters.status = @Status) order by parameters.opened_at_utc desc offset @Skip limit @Take", new { Bot = q.BotName, Symbol = q.Symbol, q.Status, q.Skip, Take = Take(q.Take) }, cancellationToken: ct))).AsList();
    }

    public async Task<IReadOnlyCollection<TradeHistoryRowDto>> GetTradesAsync(DashboardQuery q, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);

        return (await c.QueryAsync<TradeHistoryRowDto>(new CommandDefinition("select position_id PositionId, bot_name BotName, symbol Symbol, side Side, entry_price EntryPrice, (metadata->>'exitPrice')::numeric ExitPrice, quantity Quantity, realized_pnl RealizedPnl, fees Fees, (closed_at_utc-opened_at_utc) Duration, source Source, strategy_version StrategyVersion, environment Environment, close_reason CloseReason, opened_at_utc OpenedAtUtc, closed_at_utc ClosedAtUtc from trading_history.positions where closed_at_utc is not null and (@Bot is null or bot_name = @Bot) and (@Symbol is null or symbol = @Symbol) order by closed_at_utc desc offset @Skip limit @Take", new { Bot = q.BotName, Symbol = q.Symbol, q.Skip, Take = Take(q.Take) }, cancellationToken: ct))).AsList();
    }

    public async Task<AnalyticsSummaryDto> GetAnalyticsAsync(DashboardQuery q, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);

        var r = await c.QuerySingleAsync<dynamic>(new CommandDefinition("select coalesce(sum(realized_pnl), 0) TotalPnl, coalesce(sum(realized_pnl) filter(where closed_at_utc>=date_trunc('day', now() at time zone 'utc')), 0) DailyPnl, coalesce(sum(realized_pnl) filter(where closed_at_utc>=now()-interval '7 day'), 0) WeeklyPnl, coalesce(sum(realized_pnl) filter(where closed_at_utc>=now()-interval '30 day'), 0) MonthlyPnl, coalesce(100.0*count(*) filter(where realized_pnl>0)/nullif(count(*), 0), 0) WinRate, coalesce(avg(realized_pnl) filter(where realized_pnl>0), 0) AverageWin, coalesce(avg(realized_pnl) filter(where realized_pnl<0), 0) AverageLoss, coalesce(avg(extract(epoch from(closed_at_utc-opened_at_utc))/60), 0) AverageHoldingMinutes, coalesce(sum(realized_pnl) filter(where side = 'Long'), 0) LongPnl, coalesce(sum(realized_pnl) filter(where side = 'Short'), 0) ShortPnl from trading_history.positions where closed_at_utc is not null and (@Bot is null or bot_name = @Bot)", new { Bot = q.BotName }, cancellationToken: ct)); var counts = await c.QuerySingleAsync<(int Signals, int Opened, int Blocked)>(new CommandDefinition("select (select count(*)::int from trading_history.signals where (@Bot is null or bot_name = @Bot)) Signals, (select count(*)::int from trading_history.positions where (@Bot is null or bot_name = @Bot)) Opened, (select count(*)::int from trading_history.strategy_decisions where decision = 'Block' and (@Bot is null or bot_name = @Bot)) Blocked", new { Bot = q.BotName }, cancellationToken: ct)); var reasons = (await c.QueryAsync<(string Reason, int Count)>(new CommandDefinition("select coalesce(reason, 'Unknown') Reason, count(*)::int Count from trading_history.strategy_decisions where decision = 'Block' and (@Bot is null or bot_name = @Bot) group by reason order by count(*) desc", new { Bot = q.BotName }, cancellationToken: ct))).ToDictionary(x => x.Reason, x => x.Count); var dd = await c.ExecuteScalarAsync<decimal>(new CommandDefinition("select round(coalesce(max(maximum_drawdown_percent), 0), 8)::numeric(18,8) from trading.performance_snapshots where (cast(@Bot as text) is null or bot_name = cast(@Bot as text))", new { Bot = q.BotName }, cancellationToken: ct)); return new((decimal)r.totalpnl, (decimal)r.dailypnl, (decimal)r.weeklypnl, (decimal)r.monthlypnl, (decimal)r.winrate, (decimal)r.averagewin, (decimal)r.averageloss, dd, (decimal)r.averageholdingminutes, counts.Signals, counts.Opened, counts.Blocked, reasons, (decimal)r.longpnl, (decimal)r.shortpnl);
    }
    public async Task<IReadOnlyCollection<EquityPointDto>> GetEquityAsync(
        DashboardQuery q,
        CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);

        const string sql = """
            with trades as (
                select
                    closed_at_utc as time_utc,
                    coalesce(realized_pnl, 0)::numeric as pnl
                from trading_history.positions
                where closed_at_utc is not null
                  and (
                      cast(@Bot as text) is null
                      or bot_name = cast(@Bot as text)
                  )
            ),
            equity_points as (
                select
                    time_utc,
                    sum(pnl) over (
                        order by time_utc
                        rows between unbounded preceding and current row
                    ) as equity
                from trades
            ),
            peaks as (
                select
                    time_utc,
                    equity,
                    max(equity) over (
                        order by time_utc
                        rows between unbounded preceding and current row
                    ) as peak
                from equity_points
            )
            select
                time_utc as "TimeUtc",
                equity::double precision as "Equity",
                (
                    case
                        when peak is null or peak <= 0 then 0
                        else greatest(
                            0::numeric,
                            100::numeric * (peak - equity) / nullif(abs(peak), 0)
                        )
                    end
                )::double precision as "DrawdownPercent"
            from peaks
            order by time_utc;
            """;

        var rows = await c.QueryAsync<EquityDbRow>(
            new CommandDefinition(
                sql,
                new { Bot = q.BotName },
                cancellationToken: ct));

        return rows
            .Select(x => new EquityPointDto(
                x.TimeUtc,
                Convert.ToDecimal(x.Equity),
                Convert.ToDecimal(x.DrawdownPercent)))
            .ToArray();
    }

    public async Task<PriceChartDto> GetPriceChartAsync(string symbol, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        var candles = (await command.QueryAsync<PriceCandleDto>(
            new CommandDefinition("select " +
            "open_time_utc OpenTimeUtc," +
            " open Open," +
            " high High, " +
            "low Low, " +
            "close Close," +
            " volume Volume " +
            "from trading_dashboard.market_candles" +
            " where symbol = @symbol " +
            "and interval = @interval" +
            " and open_time_utc>=@fromUtc " +
            "and open_time_utc<@toUtc " +
            "order by open_time_utc " +
            "limit 5000",
            new { symbol, interval, fromUtc, toUtc },
            cancellationToken: ct)))
            .AsList(); 
        
        var markers = (await command.QueryAsync<ChartMarkerDto>(
            new CommandDefinition("select " +
            "s.signal_time_utc TimeUtc," +
            " case when d.decision = 'Block' then 'BlockedSignal' else 'Signal' end Kind," +
            " s.side Side," +
            " coalesce(s.reference_price, d.mark_price, 0) Price," +
            " coalesce(d.reason, d.decision) Label " +
            "from trading_history.signals s " +
            "left join lateral(select decision, reason, mark_price from trading_history.strategy_decisions x where x.signal_id = s.signal_id order by decided_at_utc desc limit 1)d on true" +
            " where s.symbol = @symbol " +
            "and s.signal_time_utc>=@fromUtc " +
            "and s.signal_time_utc<@toUtc " +
            "order by s.signal_time_utc", 
            new { symbol, fromUtc, toUtc },
            cancellationToken: ct)))
            .AsList(); 
        
        return new(candles, markers);
    }

    public async Task<IReadOnlyCollection<RunSummaryDto>> GetRunsAsync(DashboardQuery q, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        return (await command.QueryAsync<RunSummaryDto>(
            new CommandDefinition("select " +
            "request.run_id RunId, " +
            "request.run_type RunType, " +
            "request.bot_name BotName, " +
            "request.strategy_version StrategyVersion, " +
            "request.symbol Symbol, " +
            "request.interval Interval, " +
            "request.started_at_utc StartedAtUtc, " +
            "request.completed_at_utc CompletedAtUtc, " +
            "request.status Status, " +
            "s.net_profit NetProfit, " +
            "s.win_rate_percent WinRatePercent, " +
            "s.maximum_drawdown_percent MaxDrawdownPercent, " +
            "s.score Score " +
            "from trading.performance_runs request " +
            "left join trading.performance_snapshots s on s.run_id = request.run_id " +
            "where (@Bot is null or request.bot_name = @Bot) " +
            "order by request.started_at_utc desc " +
            "offset @Skip limit @Take", 
            new { Bot = q.BotName, q.Skip, Take = Take(q.Take) },
            cancellationToken: ct)))
            .AsList();
    }

    public async Task<IReadOnlyCollection<OptimizationTrialDto>> GetOptimizationTrialsAsync(Guid runId, int take, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        var rows = await command.QueryAsync<dynamic>(
            new CommandDefinition("select " +
            "trial_id, " +
            "parameters::text, " +
            "score, " +
            "metrics::text, " +
            "selected, " +
            "row_number() over(order by score desc) rank" +
            " from trading.optimization_trials" +
            " where optimization_run_id = @runId" +
            " order by score desc " +
            "limit @take", 
            new { runId, take = Take(take) }, 
            cancellationToken: ct)); 
        
        return rows.Select(x => 
        { 
            var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>((string)x.parameters) ?? [];
            using var metrics = JsonDocument.Parse((string)x.metrics);
            
            decimal V(string n) => metrics.RootElement.TryGetProperty(n, out var v) && v.TryGetDecimal(out var d) ? d : 0; 
            int I(string n) => metrics.RootElement.TryGetProperty(n, out var v) && v.TryGetInt32(out var d) ? d : 0; 
            
            return new OptimizationTrialDto(
                (Guid)x.trial_id,
                (int)(long)x.rank, 
                parameters, 
                (decimal)x.score,
                V("NetProfit"),
                V("MaximumDrawdownPercent"), 
                V("WinRatePercent"), 
                I("ClosedPositions"), 
                (bool)x.selected); })
            .ToArray();
    }

    public async Task<StrategyComparisonDto?> CompareRunsAsync(Guid l, Guid r, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        var rows = (await command.QueryAsync<dynamic>(new CommandDefinition(
            """           
             select 
                r.run_id, 
                r.bot_name || ' ' || 
                r.strategy_version label, 
                s.net_profit, 
                s.maximum_drawdown_percent, 
                s.win_rate_percent, 
                s.score 
             from trading.performance_runs r 
             join trading.performance_snapshots s on s.run_id = r.run_id 
             where r.run_id = any(@ids)            
            """, 
            new { ids = new[] { l, r } }, 
            cancellationToken: ct)))
            .ToDictionary(x => (Guid)x.run_id); 
        
        if (!rows.TryGetValue(l, out var a) 
            || !rows.TryGetValue(r, out var b)) 
            return null; 
        
        return new(
            l, 
            r, 
            (string)a.label, 
            (string)b.label, 
            (decimal)a.net_profit - (decimal)b.net_profit, 
            (decimal)a.maximum_drawdown_percent - (decimal)b.maximum_drawdown_percent, 
            (decimal)a.win_rate_percent - (decimal)b.win_rate_percent, 
            (decimal)a.score - (decimal)b.score);       
    }

    public async Task<IReadOnlyCollection<ComponentHealthDto>> GetHealthAsync(CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        return await GetHealthWithConnection(command, ct);
    }

    private static async Task<IReadOnlyCollection<ComponentHealthDto>> GetHealthWithConnection(System.Data.Common.DbConnection c, CancellationToken ct)
        => (await c.QueryAsync<ComponentHealthDto>(new CommandDefinition(
            """
            with latest as (
                select 
                    component, 
                    instance_id, 
                    last_seen_at_utc, 
                    stale_after_seconds, 
                    details, 
                    row_number() over(partition by component order by last_seen_at_utc desc, instance_id desc) rn 
                    from trading_dashboard.service_heartbeats) 
               select 
                component Component, 
                case when last_seen_at_utc>=now()-make_interval(secs => stale_after_seconds) then 'Healthy' else 'Unhealthy' end Status, 
                last_seen_at_utc LastSeenUtc, 
                details Details
                from latest where rn = 1 
                order by component
            """, 
            cancellationToken: ct)))
        .AsList();
            

    public async Task<IReadOnlyCollection<AlertDto>> GetAlertsAsync(bool acknowledged, int take, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        return (await command.QueryAsync<AlertDto>(
            new CommandDefinition(
                "select alert_id AlertId, " +
                    "severity Severity, " +
                    "type Type," +
                    " message Message," +
                    " bot_name BotName, " +
                    "position_id PositionId, " +
                    "created_at_utc CreatedAtUtc, " +
                    "acknowledged Acknowledged, " +
                    "acknowledged_at_utc AcknowledgedAtUtc," +
                    " acknowledged_by AcknowledgedBy" +
                    " from trading_dashboard.alerts " +
                    "where acknowledged = @acknowledged " +
                    "and not resolved " +
                    "order by created_at_utc desc " +
                    "limit @take", 
                new { acknowledged, take = Take(take) }, 
                cancellationToken: ct)))
                .AsList();
    }

    public async Task<IReadOnlyCollection<BotConfigurationDto>> GetAllAsync(CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        return (await command.QueryAsync<BotConfigurationDto>(
            new CommandDefinition("select " +
            "bot_name BotName, " +
            "strategy_type StrategyType, " +
            "symbol Symbol, " +
            "environment Environment, " +
            "signal_source SignalSource, " +
            "enable_long EnableLong, " +
            "enable_short EnableShort, " +
            "quantity Quantity, " +
            "leverage Leverage, " +
            "price_distance PriceDistance, " +
            "profit_distance ProfitDistance, " +
            "order_side_limit OrderSideLimit, " +
            "cooldown_seconds CooldownSeconds, " +
            "version Version, " +
            "updated_at_utc UpdatedAtUtc, " +
            "updated_by UpdatedBy," +
            " restart_required RestartRequired " +
            "from trading_dashboard.bot_configurations " +
            "order by bot_name",
            cancellationToken: ct)))
            .AsList();
    }

    public async Task<BotConfigurationDto?> GetAsync(string botName, CancellationToken ct)
        => (await GetAllAsync(ct)).FirstOrDefault(x => x.BotName.Equals(botName, StringComparison.OrdinalIgnoreCase));

    public async Task<BotConfigurationDto> UpdateAsync(string bot, UpdateBotConfigurationRequest request, string user, CancellationToken ct)
    {
        DashboardValidation.Validate(request);

        await using var command = await factory.OpenAsync(ct);

        var row = await command.QuerySingleOrDefaultAsync<BotConfigurationDto>(
            new CommandDefinition("update trading_dashboard.bot_configurations " +
            "set strategy_type = @StrategyType, " +
            "symbol = @Symbol, " +
            "environment = @Environment, " +
            "signal_source = @SignalSource, " +
            "enable_long = @EnableLong, " +
            "enable_short = @EnableShort, " +
            "quantity = @Quantity, " +
            "leverage = @Leverage, " +
            "price_distance = @PriceDistance, " +
            "profit_distance = @ProfitDistance, " +
            "order_side_limit = @OrderSideLimit, " +
            "cooldown_seconds = @CooldownSeconds, " +
            "version = version+1, " +
            "updated_at_utc = now(), " +
            "updated_by = @user, " +
            "restart_required = (environment<>@Environment) " +
            "where bot_name = @bot " +
            "and version = @ExpectedVersion " +
            "returning bot_name BotName, strategy_type StrategyType, symbol Symbol, environment Environment, signal_source SignalSource, enable_long EnableLong, enable_short EnableShort, quantity Quantity, leverage Leverage, price_distance PriceDistance, profit_distance ProfitDistance, order_side_limit OrderSideLimit, cooldown_seconds CooldownSeconds, version Version, updated_at_utc UpdatedAtUtc, updated_by UpdatedBy, restart_required RestartRequired", 
            new 
            { 
                bot, 
                user, 
                request.StrategyType, 
                request.Symbol,
                Environment = request.Environment.ToString(), 
                request.SignalSource, 
                request.EnableLong, 
                request.EnableShort,
                request.Quantity, 
                request.Leverage, 
                request.PriceDistance,
                request.ProfitDistance,
                request.OrderSideLimit, 
                request.CooldownSeconds, 
                request.ExpectedVersion 
            },
            cancellationToken: ct));
        
        return row ?? throw new InvalidOperationException("Configuration was changed by another user or bot was not found.");
    }

    public async Task<BotCommandDto> EnqueueAsync(string bot, BotCommandRequest request, string user, CancellationToken ct)
    {
        DashboardValidation.Validate(request);

        await using var command = await factory.OpenAsync(ct);
        var id = Guid.NewGuid();

        return await command.QuerySingleAsync<BotCommandDto>(
            new CommandDefinition("insert into trading_dashboard.bot_commands" +
            "(command_id, " +
            "bot_name, " +
            "command, " +
            "status, " +
            "requested_by, " +
            "reason, " +
            "payload) " +
            "values(@id, @bot, @command, 'Pending', @user, @reason, cast(@payload as jsonb)) " +
            "returning command_id CommandId, bot_name BotName, command Command, status Status, requested_by RequestedBy, reason Reason, requested_at_utc RequestedAtUtc, completed_at_utc CompletedAtUtc, error Error", 
            new 
            { 
                id, 
                bot, 
                command = request.Command.ToString(), 
                user, 
                reason = request.Reason,
                payload = JsonSerializer.Serialize(request)
            },
            cancellationToken: ct));
    }

    public async Task<IReadOnlyCollection<BotCommandDto>> GetAsync(string? bot, int take, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        return (await command.QueryAsync<BotCommandDto>(
            new CommandDefinition("" +
            "select " +
            "command_id CommandId, " +
            "bot_name BotName, " +
            "command Command, " +
            "status Status, " +
            "requested_by RequestedBy, " +
            "reason Reason, " +
            "requested_at_utc RequestedAtUtc, " +
            "completed_at_utc CompletedAtUtc, " +
            "error Error" +
            " from trading_dashboard.bot_commands" +
            " where (@bot is null or bot_name = @bot) " +
            "order by requested_at_utc desc " +
            "limit @take", 
            new { bot, take = Take(take) }, 
            cancellationToken: ct)))
            .AsList();
    }

    public async Task<JobAcceptedDto> EnqueueBacktestAsync(BacktestRequest r, string user, CancellationToken ct)
    {
        DashboardValidation.Validate(r);

        return await Job("Backtest", r, user, ct);
    }

    public Task<JobAcceptedDto> EnqueueOptimizationAsync(OptimizationRequest r, string user, CancellationToken ct)
        => Job("Optimization", r, user, ct);

    private async Task<JobAcceptedDto> Job(string type, object request, string user, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        var id = Guid.NewGuid();

        return await command.QuerySingleAsync<JobAcceptedDto>(
            new CommandDefinition(
                "insert into trading_dashboard.jobs" +
                    "(job_id," +
                    " type, " +
                    "status," +
                    " requested_by," +
                    " request)" +
                    " values(@id, @type, 'Pending', @user, cast(@json as jsonb))" +
                    " returning " +
                    "job_id JobId," +
                    " type Type, " +
                    "status Status, " +
                    "created_at_utc CreatedAtUtc", 
                new { id, type, user, json = JsonSerializer.Serialize(request) }, 
                cancellationToken: ct));
    }

    public async Task AcknowledgeAsync(long id, string user, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct); 
        
        await command.ExecuteAsync(new CommandDefinition(
            "update trading_dashboard.alerts " +
            "set acknowledged = true, " +
            "acknowledged_at_utc = now(), " +
            "acknowledged_by = @user " +
            "where alert_id = @id", 
            new { id, user }, 
            cancellationToken: ct));
    }

    public async Task<IReadOnlyCollection<AuditEventDto>> GetAuditEventsAsync(string? actor, string? action, DateTime? fromUtc, DateTime? toUtc, int skip, int take, CancellationToken ct)
    {
        await using var command = await factory.OpenAsync(ct);

        return (await command.QueryAsync<AuditEventDto>(
            new CommandDefinition(
                "select audit_id AuditId, " +
                    "occurred_at_utc OccurredAtUtc, " +
                    "actor Actor, " +
                    "action Action, " +
                    "entity_type EntityType, " +
                    "entity_id EntityId," +
                    " reason Reason," +
                    " correlation_id CorrelationId, " +
                    "ip_address IpAddress, " +
                    "old_value::text OldValueJson, " +
                    "new_value::text NewValueJson " +
                    "from trading_dashboard.audit_events" +
                    " where (@actor is null or actor = @actor) " +
                    "and (@action is null or action ilike '%' || @action || '%') " +
                    "and (cast(@fromUtc as timestamptz) is null " +
                    "or occurred_at_utc >= cast(@fromUtc as timestamptz)) " +
                    "and (cast(@toUtc as timestamptz) is null " +
                    "or occurred_at_utc < cast(@toUtc as timestamptz)) " +
                    "order by occurred_at_utc desc " +
                    "offset @skip limit @take", 
                new { actor, action, fromUtc, toUtc, skip, take = Take(take) },
                cancellationToken: ct)))
                .AsList();
    }

    private sealed record OverviewTotalsRow(
        decimal Realized,
        decimal Unrealized,
        int Positions,
        int Alerts);

    private sealed record EquityDbRow(
        DateTime TimeUtc,
        double Equity,
        double DrawdownPercent);
}
