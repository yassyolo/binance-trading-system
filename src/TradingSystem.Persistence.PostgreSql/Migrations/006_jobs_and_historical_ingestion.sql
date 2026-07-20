ALTER TABLE trading_dashboard.jobs
    ADD COLUMN IF NOT EXISTS processing_worker_id varchar(200), 
    ADD COLUMN IF NOT EXISTS processing_started_at_utc timestamptz, 
    ADD COLUMN IF NOT EXISTS attempt_count integer NOT NULL DEFAULT 0, 
    ADD COLUMN IF NOT EXISTS next_attempt_at_utc timestamptz, 
    ADD COLUMN IF NOT EXISTS progress_percent integer NOT NULL DEFAULT 0, 
    ADD COLUMN IF NOT EXISTS progress_stage varchar(200);

CREATE INDEX IF NOT EXISTS ix_dashboard_jobs_claim
ON trading_dashboard.jobs(type, status, next_attempt_at_utc, created_at_utc)
WHERE status IN ('Pending', 'Processing');

CREATE TABLE IF NOT EXISTS trading_dashboard.historical_data_gaps
(
    symbol varchar(32) NOT NULL, 
    interval varchar(16) NOT NULL, 
    gap_from_utc timestamptz NOT NULL, 
    gap_to_utc timestamptz NOT NULL, 
    missing_candles integer NOT NULL, 
    detected_at_utc timestamptz NOT NULL DEFAULT now(), 
    PRIMARY KEY(symbol, interval, gap_from_utc)
);

CREATE OR REPLACE FUNCTION trading_dashboard.interval_duration(value text)
RETURNS interval LANGUAGE plpgsql IMMUTABLE AS $$
DECLARE amount integer; unit text;
BEGIN
    amount : =  substring(value from '^[0-9]+')::integer;
    unit : =  right(value, 1);
    IF unit  =  'm' THEN RETURN make_interval(mins  =>  amount); END IF;
    IF unit  =  'h' THEN RETURN make_interval(hours  =>  amount); END IF;
    IF unit  =  'd' THEN RETURN make_interval(days  =>  amount); END IF;
    RAISE EXCEPTION 'Unsupported interval: %',  value;
END;
$$;
