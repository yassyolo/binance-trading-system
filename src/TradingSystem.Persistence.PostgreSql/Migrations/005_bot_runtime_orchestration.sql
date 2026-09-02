ALTER TABLE trading_dashboard.bot_configurations
    ADD COLUMN IF NOT EXISTS runtime_version bigint NOT NULL DEFAULT 1, 
    ADD COLUMN IF NOT EXISTS runtime_updated_at_utc timestamptz NOT NULL DEFAULT now(), 
    ADD COLUMN IF NOT EXISTS runtime_updated_by varchar(128) NOT NULL DEFAULT 'migration', 
    ADD COLUMN IF NOT EXISTS runtime_reason text, 
    ADD COLUMN IF NOT EXISTS execution_enabled boolean NOT NULL DEFAULT false;

UPDATE trading_dashboard.bot_configurations
SET runtime_status = CASE WHEN runtime_status IN ('Running', 'Paused', 'Stopped', 'EmergencyStopped', 'Faulted') THEN runtime_status ELSE 'Stopped' END;

ALTER TABLE trading_dashboard.bot_commands
    ADD COLUMN IF NOT EXISTS processing_worker_id varchar(128), 
    ADD COLUMN IF NOT EXISTS attempt_count integer NOT NULL DEFAULT 0, 
    ADD COLUMN IF NOT EXISTS next_attempt_at_utc timestamptz NOT NULL DEFAULT now();

CREATE INDEX IF NOT EXISTS ix_bot_commands_claim
    ON trading_dashboard.bot_commands(status,  next_attempt_at_utc,  requested_at_utc)
    WHERE status IN ('Pending', 'Processing');

CREATE TABLE IF NOT EXISTS trading_dashboard.bot_configuration_refresh_log(
    refresh_id bigserial PRIMARY KEY, 
    bot_name varchar(64) NOT NULL, 
    configuration_version bigint NOT NULL, 
    refreshed_at_utc timestamptz NOT NULL DEFAULT now(), 
    service_instance varchar(128) NOT NULL, 
    result varchar(24) NOT NULL, 
    details text
);

CREATE INDEX IF NOT EXISTS ix_bot_configuration_refresh_log_bot
    ON trading_dashboard.bot_configuration_refresh_log(bot_name,  refreshed_at_utc DESC);
