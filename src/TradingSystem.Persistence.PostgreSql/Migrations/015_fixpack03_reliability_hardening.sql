BEGIN;

-- Preserve terminal worker ownership after processing_worker_id is cleared.
ALTER TABLE trading_dashboard.bot_commands
    ADD COLUMN IF NOT EXISTS completed_by_worker_id varchar(200);

ALTER TABLE trading_dashboard.jobs
    ADD COLUMN IF NOT EXISTS completed_by_worker_id varchar(200);

ALTER TABLE trading_replay.jobs
    ADD COLUMN IF NOT EXISTS completed_by_worker_id varchar(300);

CREATE INDEX IF NOT EXISTS ix_bot_commands_completed_worker
    ON trading_dashboard.bot_commands(completed_by_worker_id, completed_at_utc DESC)
    WHERE completed_by_worker_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_dashboard_jobs_completed_worker
    ON trading_dashboard.jobs(completed_by_worker_id, completed_at_utc DESC)
    WHERE completed_by_worker_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_replay_jobs_completed_worker
    ON trading_replay.jobs(completed_by_worker_id, completed_at_utc DESC)
    WHERE completed_by_worker_id IS NOT NULL;

-- Canonical service-level health projection: one latest instance per component.
CREATE OR REPLACE VIEW trading_dashboard.v_service_health AS
WITH latest AS (
    SELECT
        component,
        instance_id,
        version,
        environment,
        status AS reported_status,
        started_at_utc,
        last_seen_at_utc,
        stale_after_seconds,
        details,
        row_number() OVER (
            PARTITION BY component
            ORDER BY last_seen_at_utc DESC, instance_id DESC
        ) AS rn
    FROM trading_dashboard.service_heartbeats
)
SELECT
    component,
    instance_id,
    version,
    environment,
    CASE
        WHEN last_seen_at_utc >= now() - make_interval(secs => stale_after_seconds)
            THEN 'Healthy'
        ELSE 'Unhealthy'
    END AS status,
    reported_status,
    started_at_utc,
    last_seen_at_utc,
    stale_after_seconds,
    details
FROM latest
WHERE rn = 1;

-- Old alert keys were instance-scoped and never disappeared because historical
-- heartbeat rows remain append-like. The alert source is now component-scoped.
UPDATE trading_dashboard.alerts
SET resolved = true,
    resolved_at_utc = COALESCE(resolved_at_utc, now())
WHERE NOT resolved
  AND type = 'ServiceHeartbeatMissing'
  AND deduplication_key ~ '^operational:heartbeat:[^:]+:[^:]+$';

-- Durable migration ledger used by scripts/Apply-TradingMigrations.ps1.
-- The runner writes one row only after the corresponding SQL file succeeds.
CREATE TABLE IF NOT EXISTS trading_dashboard.schema_migrations
(
    migration_name varchar(255) PRIMARY KEY,
    checksum_sha256 varchar(64) NULL,
    applied_at_utc timestamptz NOT NULL DEFAULT now(),
    applied_by varchar(200) NOT NULL DEFAULT 'manual',
    notes text NULL
);

COMMIT;
