CREATE SCHEMA IF NOT EXISTS trading_dashboard;
CREATE TABLE IF NOT EXISTS trading_dashboard.bot_configurations(
 bot_name varchar(64) PRIMARY KEY, strategy_type varchar(64) NOT NULL, symbol varchar(32) NOT NULL, 
 environment varchar(16) NOT NULL CHECK(environment IN('Demo', 'Production')), signal_source varchar(64) NOT NULL, 
 enable_long boolean NOT NULL, enable_short boolean NOT NULL, quantity numeric(28, 10) NOT NULL CHECK(quantity>0), 
 leverage integer NOT NULL CHECK(leverage between 1 and 125), price_distance numeric(28, 10), profit_distance numeric(28, 10), 
 order_side_limit integer, cooldown_seconds integer NOT NULL CHECK(cooldown_seconds>=0), runtime_status varchar(16) NOT NULL DEFAULT 'Stopped', 
 version bigint NOT NULL DEFAULT 1, updated_at_utc timestamptz NOT NULL DEFAULT now(), updated_by varchar(128) NOT NULL DEFAULT 'migration', restart_required boolean NOT NULL DEFAULT false, 
 CHECK(price_distance is null or price_distance>0), CHECK(profit_distance is null or profit_distance>0), CHECK(order_side_limit is null or order_side_limit>0));
CREATE TABLE IF NOT EXISTS trading_dashboard.bot_commands(command_id uuid PRIMARY KEY, bot_name varchar(64) NOT NULL, command varchar(32) NOT NULL, status varchar(24) NOT NULL, requested_by varchar(128) NOT NULL, reason text NOT NULL, payload jsonb NOT NULL DEFAULT '{}'::jsonb, requested_at_utc timestamptz NOT NULL DEFAULT now(), processing_started_at_utc timestamptz, completed_at_utc timestamptz, error text, idempotency_key varchar(128));
CREATE UNIQUE INDEX IF NOT EXISTS ux_bot_commands_idempotency ON trading_dashboard.bot_commands(idempotency_key) WHERE idempotency_key IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_bot_commands_pending ON trading_dashboard.bot_commands(status, requested_at_utc) WHERE status = 'Pending';
CREATE TABLE IF NOT EXISTS trading_dashboard.service_heartbeats(component varchar(96) PRIMARY KEY, status varchar(24) NOT NULL DEFAULT 'Healthy', last_seen_utc timestamptz NOT NULL, stale_after_seconds integer NOT NULL DEFAULT 60, details text, metadata jsonb NOT NULL DEFAULT '{}'::jsonb);
CREATE TABLE IF NOT EXISTS trading_dashboard.alerts(alert_id bigserial PRIMARY KEY, severity varchar(16) NOT NULL, type varchar(64) NOT NULL, message text NOT NULL, bot_name varchar(64), position_id varchar(64), created_at_utc timestamptz NOT NULL DEFAULT now(), acknowledged boolean NOT NULL DEFAULT false, acknowledged_at_utc timestamptz, acknowledged_by varchar(128), metadata jsonb NOT NULL DEFAULT '{}'::jsonb);
CREATE INDEX IF NOT EXISTS ix_alerts_open ON trading_dashboard.alerts(severity, created_at_utc desc) WHERE not acknowledged;
CREATE TABLE IF NOT EXISTS trading_dashboard.jobs(job_id uuid PRIMARY KEY, type varchar(32) NOT NULL, status varchar(24) NOT NULL, requested_by varchar(128) NOT NULL, request jsonb NOT NULL, result_run_id uuid, error text, created_at_utc timestamptz NOT NULL DEFAULT now(), started_at_utc timestamptz, completed_at_utc timestamptz);
CREATE INDEX IF NOT EXISTS ix_dashboard_jobs_pending ON trading_dashboard.jobs(status, created_at_utc) WHERE status = 'Pending';
CREATE TABLE IF NOT EXISTS trading_dashboard.market_candles(symbol varchar(32) NOT NULL, interval varchar(16) NOT NULL, open_time_utc timestamptz NOT NULL, open numeric(28, 10) NOT NULL, high numeric(28, 10) NOT NULL, low numeric(28, 10) NOT NULL, close numeric(28, 10) NOT NULL, volume numeric(28, 10) NOT NULL, PRIMARY KEY(symbol, interval, open_time_utc));

CREATE OR REPLACE VIEW trading_dashboard.v_live_bot_overview AS
SELECT c.bot_name, c.runtime_status status, c.environment, c.signal_source, 
 ls.side last_signal_side, ls.signal_time_utc last_signal_at_utc, ld.decision last_decision, ld.reason last_decision_reason, ld.decided_at_utc last_decision_at_utc, 
 coalesce(p.open_positions, 0)::int open_positions, coalesce(p.unrealized_pnl, 0) unrealized_pnl, coalesce(d.realized_pnl_today, 0) realized_pnl_today, 
 coalesce(ls.strategy_version, 'unknown') strategy_version, h.last_seen_utc last_heartbeat_utc
FROM trading_dashboard.bot_configurations c
LEFT JOIN LATERAL(SELECT side, signal_time_utc, strategy_version FROM trading_history.signals s WHERE s.bot_name = c.bot_name ORDER BY signal_time_utc DESC LIMIT 1)ls ON true
LEFT JOIN LATERAL(SELECT decision, reason, decided_at_utc FROM trading_history.strategy_decisions x WHERE x.bot_name = c.bot_name ORDER BY decided_at_utc DESC LIMIT 1)ld ON true
LEFT JOIN LATERAL(SELECT count(*) open_positions, 0::numeric unrealized_pnl FROM trading_history.positions x WHERE x.bot_name = c.bot_name AND x.closed_at_utc IS NULL)p ON true
LEFT JOIN LATERAL(SELECT coalesce(sum(realized_pnl), 0) realized_pnl_today FROM trading_history.positions x WHERE x.bot_name = c.bot_name AND x.closed_at_utc>=date_trunc('day', now() at time zone 'utc'))d ON true
LEFT JOIN trading_dashboard.service_heartbeats h ON h.component = c.bot_name;

INSERT INTO trading_dashboard.bot_configurations(bot_name, strategy_type, symbol, environment, signal_source, enable_long, enable_short, quantity, leverage, price_distance, profit_distance, order_side_limit, cooldown_seconds)
VALUES
('BOT8011', 'ProtectedStop3', 'BTCUSDC', 'Demo', 'TradingView', true, true, 0.002, 50, null, null, 1, 180), 
('BOT8012', 'TpOnlyGrid', 'BTCUSDC', 'Demo', 'TradingView', true, true, 0.002, 50, 400, 200, 2, 180), 
('BOT8013', 'TpOnlyGrid', 'BTCUSDC', 'Demo', 'Internal', true, true, 0.002, 50, 400, 200, 2, 180), 
('BOT8014', 'TpOnlyGrid', 'BTCUSDC', 'Demo', 'Internal', true, true, 0.002, 50, 400, 200, 2, 180), 
('BOT8015', 'ProtectedStop3', 'BTCUSDC', 'Demo', 'Internal', true, true, 0.002, 50, null, null, 1, 180), 
('BOT8016', 'AlligatorMultiTimeframe', 'BTCUSDC', 'Demo', 'Internal', true, true, 0.002, 50, null, null, 1, 180)
ON CONFLICT(bot_name) DO NOTHING;
