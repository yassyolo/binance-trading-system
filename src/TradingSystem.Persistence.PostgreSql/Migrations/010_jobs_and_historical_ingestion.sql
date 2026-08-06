BEGIN;

ALTER TABLE trading_dashboard.jobs
    ADD COLUMN IF NOT EXISTS processing_worker_id varchar(200),
    ADD COLUMN IF NOT EXISTS processing_started_at_utc timestamptz,
    ADD COLUMN IF NOT EXISTS attempt_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS next_attempt_at_utc timestamptz,
    ADD COLUMN IF NOT EXISTS progress_percent integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS progress_stage varchar(200);

CREATE INDEX IF NOT EXISTS ix_dashboard_jobs_claim
    ON trading_dashboard.jobs
    (
        type,
        status,
        next_attempt_at_utc,
        created_at_utc
    )
    WHERE status IN ('Pending', 'Processing');

CREATE TABLE IF NOT EXISTS trading_dashboard.historical_data_gaps
(
    symbol varchar(32) NOT NULL,
    interval varchar(16) NOT NULL,
    gap_from_utc timestamptz NOT NULL,
    gap_to_utc timestamptz NOT NULL,
    missing_candles integer NOT NULL,
    detected_at_utc timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT historical_data_gaps_pkey
        PRIMARY KEY (symbol, interval, gap_from_utc),

    CONSTRAINT ck_historical_data_gaps_range
        CHECK (gap_to_utc >= gap_from_utc),

    CONSTRAINT ck_historical_data_gaps_missing
        CHECK (missing_candles > 0)
);

CREATE INDEX IF NOT EXISTS ix_historical_data_gaps_lookup
    ON trading_dashboard.historical_data_gaps
    (
        symbol,
        interval,
        gap_from_utc,
        gap_to_utc
    );

CREATE OR REPLACE FUNCTION trading_dashboard.interval_duration(value text)
RETURNS interval
LANGUAGE plpgsql
IMMUTABLE
STRICT
AS $$
DECLARE
    amount integer;
    unit text;
BEGIN
    IF value !~ '^[0-9]+[mhd]$' THEN
        RAISE EXCEPTION 'Unsupported interval: %', value;
    END IF;

    amount := substring(value from '^[0-9]+')::integer;
    unit := right(value, 1);

    RETURN CASE unit
        WHEN 'm' THEN make_interval(mins => amount)
        WHEN 'h' THEN make_interval(hours => amount)
        WHEN 'd' THEN make_interval(days => amount)
        ELSE NULL
    END;
END;
$$;

COMMIT;