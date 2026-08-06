CREATE SCHEMA IF NOT EXISTS trading_event_store;

CREATE TABLE IF NOT EXISTS trading_event_store.events
(
    global_position BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY, 
    event_id UUID NOT NULL UNIQUE, 
    event_type VARCHAR(150) NOT NULL, 
    event_version INTEGER NOT NULL DEFAULT 1 CHECK (event_version > 0), 
    aggregate_type VARCHAR(100) NOT NULL, 
    aggregate_id VARCHAR(200) NOT NULL, 
    aggregate_version BIGINT NOT NULL CHECK (aggregate_version > 0), 
    occurred_at_utc TIMESTAMPTZ NOT NULL, 
    recorded_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(), 
    bot_name VARCHAR(100) NULL, 
    symbol VARCHAR(30) NULL, 
    position_id VARCHAR(150) NULL, 
    signal_id VARCHAR(150) NULL, 
    correlation_id VARCHAR(150) NULL, 
    causation_id VARCHAR(150) NULL, 
    actor VARCHAR(200) NULL, 
    payload JSONB NOT NULL DEFAULT '{}'::jsonb, 
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb, 
    CONSTRAINT uq_event_stream_version UNIQUE (aggregate_type,  aggregate_id,  aggregate_version)
);

CREATE INDEX IF NOT EXISTS ix_events_occurred_at ON trading_event_store.events (occurred_at_utc DESC);
CREATE INDEX IF NOT EXISTS ix_events_bot_timeline ON trading_event_store.events (bot_name,  occurred_at_utc DESC) WHERE bot_name IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_events_position_timeline ON trading_event_store.events (position_id,  global_position) WHERE position_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_events_signal_timeline ON trading_event_store.events (signal_id,  global_position) WHERE signal_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_events_correlation ON trading_event_store.events (correlation_id,  global_position) WHERE correlation_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_events_event_type ON trading_event_store.events (event_type,  occurred_at_utc DESC);
CREATE INDEX IF NOT EXISTS ix_events_payload_gin ON trading_event_store.events USING GIN (payload jsonb_path_ops);

COMMENT ON TABLE trading_event_store.events IS
'Append-only domain and integration event log. Operational state tables remain the read-model source of truth; this table provides traceability,  replay input and timeline reconstruction.';

REVOKE UPDATE,  DELETE,  TRUNCATE ON trading_event_store.events FROM PUBLIC;
