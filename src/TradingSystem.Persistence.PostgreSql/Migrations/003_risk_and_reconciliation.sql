/*CREATE SCHEMA IF NOT EXISTS trading;
CREATE TABLE IF NOT EXISTS trading.risk_events(
 id uuid PRIMARY KEY,  occurred_at_utc timestamptz NOT NULL,  signal_id text NULL,  bot_name text NOT NULL,  symbol text NOT NULL, 
 decision text NOT NULL,  code text NOT NULL,  reason text NOT NULL,  context jsonb NOT NULL DEFAULT '{}'::jsonb);
CREATE INDEX IF NOT EXISTS ix_risk_events_bot_time ON trading.risk_events(bot_name,  occurred_at_utc DESC);
CREATE TABLE IF NOT EXISTS trading.reconciliation_runs(
 id bigserial PRIMARY KEY,  started_at_utc timestamptz NOT NULL,  completed_at_utc timestamptz NOT NULL,  finding_count int NOT NULL,  healed_count int NOT NULL);
CREATE TABLE IF NOT EXISTS trading.reconciliation_findings(
 id uuid PRIMARY KEY,  run_id bigint NOT NULL REFERENCES trading.reconciliation_runs(id) ON DELETE CASCADE, 
 detected_at_utc timestamptz NOT NULL,  bot_name text NOT NULL,  symbol text NOT NULL,  short_id text NULL, 
 finding_type text NOT NULL,  severity text NOT NULL,  details text NOT NULL,  suggested_action text NOT NULL, 
 auto_heal_allowed boolean NOT NULL,  resolved boolean NOT NULL DEFAULT false,  resolved_at_utc timestamptz NULL);
CREATE INDEX IF NOT EXISTS ix_reconciliation_unresolved ON trading.reconciliation_findings(resolved, severity, detected_at_utc DESC);*/
