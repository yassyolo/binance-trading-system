/*BEGIN;

CREATE SCHEMA IF NOT EXISTS trading_replay;

CREATE TABLE IF NOT EXISTS trading_replay.jobs (
    replay_id uuid PRIMARY KEY, 
    name varchar(150) NOT NULL, 
    mode varchar(40) NOT NULL, 
    status varchar(30) NOT NULL, 
    requested_by varchar(200) NOT NULL, 
    request jsonb NOT NULL, 
    last_global_position bigint NOT NULL DEFAULT 0, 
    processed_events bigint NOT NULL DEFAULT 0, 
    failed_events bigint NOT NULL DEFAULT 0, 
    progress_percent integer NOT NULL DEFAULT 0, 
    progress_stage varchar(300), 
    error text, 
    deterministic_hash varchar(128), 
    processing_worker_id varchar(300), 
    processing_started_at_utc timestamptz, 
    attempt_count integer NOT NULL DEFAULT 0, 
    cancellation_requested boolean NOT NULL DEFAULT false, 
    cancelled_by varchar(200), 
    created_at_utc timestamptz NOT NULL DEFAULT now(), 
    started_at_utc timestamptz, 
    completed_at_utc timestamptz, 
    CONSTRAINT ck_replay_job_status CHECK (status IN ('Pending', 'Processing', 'Paused', 'Completed', 'Failed', 'Cancelled')), 
    CONSTRAINT ck_replay_mode CHECK (mode IN ('Timeline', 'Projection', 'StrategyComparison')), 
    CONSTRAINT ck_replay_progress CHECK (progress_percent BETWEEN 0 AND 100)
);

CREATE INDEX IF NOT EXISTS ix_replay_jobs_claim
    ON trading_replay.jobs(status,  created_at_utc)
    WHERE status IN ('Pending', 'Processing');

CREATE TABLE IF NOT EXISTS trading_replay.checkpoints (
    replay_id uuid PRIMARY KEY REFERENCES trading_replay.jobs(replay_id) ON DELETE CASCADE, 
    last_global_position bigint NOT NULL, 
    state jsonb NOT NULL, 
    updated_at_utc timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS trading_replay.steps (
    replay_id uuid NOT NULL REFERENCES trading_replay.jobs(replay_id) ON DELETE CASCADE, 
    global_position bigint NOT NULL, 
    source_event_id uuid NOT NULL, 
    event_type varchar(200) NOT NULL, 
    virtual_time_utc timestamptz NOT NULL, 
    succeeded boolean NOT NULL, 
    result jsonb NOT NULL, 
    error text, 
    created_at_utc timestamptz NOT NULL DEFAULT now(), 
    PRIMARY KEY(replay_id,  global_position)
);

CREATE INDEX IF NOT EXISTS ix_replay_steps_event
    ON trading_replay.steps(replay_id,  event_type,  global_position);

CREATE TABLE IF NOT EXISTS trading_replay.results (
    replay_id uuid PRIMARY KEY REFERENCES trading_replay.jobs(replay_id) ON DELETE CASCADE, 
    summary jsonb NOT NULL, 
    deterministic_hash varchar(128) NOT NULL, 
    created_at_utc timestamptz NOT NULL DEFAULT now()
);

REVOKE UPDATE,  DELETE,  TRUNCATE ON trading_replay.steps FROM PUBLIC;
REVOKE UPDATE,  DELETE,  TRUNCATE ON trading_replay.results FROM PUBLIC;

COMMIT;
*/