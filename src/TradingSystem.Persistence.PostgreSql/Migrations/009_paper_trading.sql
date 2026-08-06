CREATE SCHEMA IF NOT EXISTS trading_paper;

CREATE TABLE IF NOT EXISTS trading_paper.positions (
    position_id UUID PRIMARY KEY, 
    short_id VARCHAR(32) NOT NULL, 
    bot_name VARCHAR(50) NOT NULL, 
    symbol VARCHAR(30) NOT NULL, 
    side SMALLINT NOT NULL, 
    quantity NUMERIC(28, 12) NOT NULL CHECK (quantity > 0), 
    entry_price NUMERIC(28, 12) NOT NULL CHECK (entry_price > 0), 
    take_profit_price NUMERIC(28, 12) NOT NULL CHECK (take_profit_price > 0), 
    stop_loss_price NUMERIC(28, 12) NOT NULL CHECK (stop_loss_price > 0), 
    entry_fee NUMERIC(28, 12) NOT NULL DEFAULT 0, 
    exit_price NUMERIC(28, 12) NULL, 
    exit_fee NUMERIC(28, 12) NULL, 
    realized_pnl NUMERIC(28, 12) NULL, 
    status SMALLINT NOT NULL, 
    source VARCHAR(100) NOT NULL, 
    opened_at_utc TIMESTAMPTZ NOT NULL, 
    closed_at_utc TIMESTAMPTZ NULL, 
    close_reason VARCHAR(100) NULL, 
    version BIGINT NOT NULL DEFAULT 1, 
    archived BOOLEAN NOT NULL DEFAULT FALSE, 
    CONSTRAINT uq_paper_position_short_id UNIQUE(bot_name,  short_id)
);
CREATE INDEX IF NOT EXISTS ix_paper_positions_open ON trading_paper.positions(symbol,  opened_at_utc) WHERE status = 1 AND archived = false;
CREATE INDEX IF NOT EXISTS ix_paper_positions_history ON trading_paper.positions(bot_name,  opened_at_utc DESC) WHERE archived = false;

CREATE TABLE IF NOT EXISTS trading_paper.reset_events (
    reset_id BIGSERIAL PRIMARY KEY, 
    actor VARCHAR(200) NOT NULL, 
    reset_at_utc TIMESTAMPTZ NOT NULL, 
    positions_archived INTEGER NOT NULL
);

ALTER TABLE trading_dashboard.bot_configurations
    DROP CONSTRAINT IF EXISTS ck_bot_configuration_environment;
ALTER TABLE trading_dashboard.bot_configurations
    DROP CONSTRAINT IF EXISTS bot_configurations_environment_check;
ALTER TABLE trading_dashboard.bot_configurations
    ADD CONSTRAINT ck_bot_configuration_environment CHECK (environment IN ('Paper', 'Demo', 'Production'));


begin;

alter table trading_paper.positions
    add column if not exists signal_id text null;

alter table trading_paper.positions
    add column if not exists strategy_version text not null default 'unknown';

create index if not exists ix_trading_paper_positions_signal_id
    on trading_paper.positions(signal_id)
    where signal_id is not null;

-- Existing history rows cannot always be backfilled safely because older paper rows did not
-- persist the originating signal. New positions will be linked automatically after deployment.

commit;
