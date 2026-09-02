CREATE SCHEMA IF NOT EXISTS trading;

CREATE TABLE IF NOT EXISTS trading.performance_runs (
    run_id uuid PRIMARY KEY, 
    run_type varchar(32) NOT NULL, 
    bot_name varchar(64) NOT NULL, 
    strategy_version varchar(32) NOT NULL, 
    symbol varchar(32) NOT NULL, 
    interval varchar(16) NOT NULL, 
    started_at_utc timestamptz NOT NULL, 
    completed_at_utc timestamptz NULL, 
    status varchar(24) NOT NULL, 
    parameters jsonb NOT NULL DEFAULT '{}'::jsonb, 
    parent_run_id varchar(64) NULL, 
    notes text NULL
);

CREATE TABLE IF NOT EXISTS trading.performance_snapshots (
    run_id uuid PRIMARY KEY REFERENCES trading.performance_runs(run_id) ON DELETE CASCADE, 
    bot_name varchar(64) NOT NULL, 
    symbol varchar(32) NOT NULL, 
    period_from_utc timestamptz NOT NULL, 
    period_to_utc timestamptz NOT NULL, 
    signals integer NOT NULL, 
    opened_positions integer NOT NULL, 
    blocked_signals integer NOT NULL, 
    closed_positions integer NOT NULL, 
    winning_positions integer NOT NULL, 
    losing_positions integer NOT NULL, 
    initial_balance numeric(28, 10) NOT NULL, 
    final_balance numeric(28, 10) NOT NULL, 
    net_profit numeric(28, 10) NOT NULL, 
    return_percent numeric(18, 8) NOT NULL, 
    win_rate_percent numeric(18, 8) NOT NULL, 
    profit_factor numeric(28, 10) NOT NULL, 
    maximum_drawdown_amount numeric(28, 10) NOT NULL, 
    maximum_drawdown_percent numeric(18, 8) NOT NULL, 
    total_fees numeric(28, 10) NOT NULL, 
    expectancy numeric(28, 10) NOT NULL, 
    score numeric(28, 10) NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS trading.performance_trades (
    id bigserial PRIMARY KEY, 
    run_id uuid NOT NULL REFERENCES trading.performance_runs(run_id) ON DELETE CASCADE, 
    position_id varchar(64) NOT NULL, 
    side varchar(16) NOT NULL, 
    entry_time_utc timestamptz NOT NULL, 
    entry_price numeric(28, 10) NOT NULL, 
    exit_time_utc timestamptz NOT NULL, 
    exit_price numeric(28, 10) NOT NULL, 
    quantity numeric(28, 10) NOT NULL, 
    gross_pnl numeric(28, 10) NOT NULL, 
    fees numeric(28, 10) NOT NULL, 
    net_pnl numeric(28, 10) NOT NULL, 
    exit_reason varchar(128) NOT NULL, 
    partial_take_profit_reached boolean NOT NULL DEFAULT false, 
    UNIQUE(run_id,  position_id)
);

CREATE TABLE IF NOT EXISTS trading.optimization_trials (
    trial_id uuid PRIMARY KEY, 
    optimization_run_id uuid NOT NULL REFERENCES trading.performance_runs(run_id) ON DELETE CASCADE, 
    sequence integer NOT NULL, 
    parameters jsonb NOT NULL, 
    score numeric(28, 10) NOT NULL, 
    metrics jsonb NOT NULL, 
    selected boolean NOT NULL DEFAULT false, 
    UNIQUE(optimization_run_id,  sequence)
);

CREATE TABLE IF NOT EXISTS trading.walk_forward_windows (
    window_id uuid PRIMARY KEY, 
    run_id uuid NOT NULL REFERENCES trading.performance_runs(run_id) ON DELETE CASCADE, 
    window_number integer NOT NULL, 
    train_from_utc timestamptz NOT NULL, 
    train_to_utc timestamptz NOT NULL, 
    test_from_utc timestamptz NOT NULL, 
    test_to_utc timestamptz NOT NULL, 
    selected_parameters jsonb NOT NULL, 
    in_sample_score numeric(28, 10) NOT NULL, 
    out_of_sample_score numeric(28, 10) NOT NULL, 
    in_sample_metrics jsonb NOT NULL, 
    out_of_sample_metrics jsonb NOT NULL, 
    UNIQUE(run_id,  window_number)
);

CREATE INDEX IF NOT EXISTS ix_performance_runs_bot_started ON trading.performance_runs(bot_name,  started_at_utc DESC);
CREATE INDEX IF NOT EXISTS ix_performance_runs_type_status ON trading.performance_runs(run_type,  status);
CREATE INDEX IF NOT EXISTS ix_performance_snapshots_bot_period ON trading.performance_snapshots(bot_name,  period_to_utc DESC);
CREATE INDEX IF NOT EXISTS ix_performance_trades_run_exit ON trading.performance_trades(run_id,  exit_time_utc);
CREATE INDEX IF NOT EXISTS ix_optimization_trials_run_score ON trading.optimization_trials(optimization_run_id,  score DESC);

CREATE OR REPLACE VIEW trading.v_bot_performance_summary AS
SELECT
    r.bot_name, 
    r.symbol, 
    r.run_type, 
    count(*) AS runs, 
    avg(s.return_percent) AS average_return_percent, 
    avg(s.win_rate_percent) AS average_win_rate_percent, 
    avg(s.profit_factor) AS average_profit_factor, 
    max(s.maximum_drawdown_percent) AS worst_drawdown_percent, 
    sum(s.net_profit) AS total_net_profit, 
    sum(s.total_fees) AS total_fees, 
    max(r.completed_at_utc) AS last_completed_at_utc
FROM trading.performance_runs r
JOIN trading.performance_snapshots s ON s.run_id = r.run_id
WHERE r.status = 'Completed'
GROUP BY r.bot_name,  r.symbol,  r.run_type;
