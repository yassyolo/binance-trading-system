create schema if not exists trading_dashboard;

-- Migration 004 originally used last_seen_utc while the runtime store uses
-- last_seen_at_utc. Normalize the schema without losing existing values.
do $$
begin
    if exists (
        select 1 from information_schema.columns
        where table_schema = 'trading_dashboard'
          and table_name = 'service_heartbeats'
          and column_name = 'last_seen_utc'
    ) and not exists (
        select 1 from information_schema.columns
        where table_schema = 'trading_dashboard'
          and table_name = 'service_heartbeats'
          and column_name = 'last_seen_at_utc'
    ) then
        alter table trading_dashboard.service_heartbeats
            rename column last_seen_utc to last_seen_at_utc;
    end if;
end $$;

alter table trading_dashboard.service_heartbeats
 add column if not exists instance_id varchar(200) not null default 'legacy',
 add column if not exists version varchar(50),
 add column if not exists environment varchar(30),
 add column if not exists started_at_utc timestamptz,
 add column if not exists last_seen_at_utc timestamptz,
 add column if not exists stale_after_seconds int not null default 30;

update trading_dashboard.service_heartbeats
set last_seen_at_utc = coalesce(last_seen_at_utc, now())
where last_seen_at_utc is null;

alter table trading_dashboard.service_heartbeats
    alter column last_seen_at_utc set not null;

do $$
begin
    if exists (
        select 1 from pg_constraint
        where conrelid = 'trading_dashboard.service_heartbeats'::regclass
          and contype = 'p'
    ) then
        alter table trading_dashboard.service_heartbeats drop constraint service_heartbeats_pkey;
    end if;
exception when undefined_object then
    null;
end $$;

create unique index if not exists ux_service_heartbeats_component_instance
    on trading_dashboard.service_heartbeats(component, instance_id);

alter table trading_dashboard.alerts
 add column if not exists deduplication_key varchar(500),
 add column if not exists resolved boolean not null default false,
 add column if not exists resolved_at_utc timestamptz,
 add column if not exists last_seen_at_utc timestamptz not null default now(),
 add column if not exists occurrence_count bigint not null default 1;

update trading_dashboard.alerts
set deduplication_key = 'legacy:' || alert_id
where deduplication_key is null;

alter table trading_dashboard.alerts
    alter column deduplication_key set not null;

create unique index if not exists ux_alerts_deduplication_key
    on trading_dashboard.alerts(deduplication_key);
create index if not exists ix_alerts_active_severity
    on trading_dashboard.alerts(resolved, severity, last_seen_at_utc desc);

create table if not exists trading_dashboard.audit_events(
 audit_id uuid primary key,
 occurred_at_utc timestamptz not null,
 actor varchar(200) not null,
 action varchar(300) not null,
 entity_type varchar(100) not null,
 entity_id varchar(300),
 reason text,
 correlation_id varchar(200),
 ip_address varchar(100),
 old_value jsonb,
 new_value jsonb,
 metadata jsonb not null default '{}'::jsonb
);
create index if not exists ix_audit_events_time on trading_dashboard.audit_events(occurred_at_utc desc);
create index if not exists ix_audit_events_entity on trading_dashboard.audit_events(entity_type, entity_id, occurred_at_utc desc);
create index if not exists ix_audit_events_actor on trading_dashboard.audit_events(actor, occurred_at_utc desc);

-- One dashboard row per bot even when several service instances publish heartbeats.
create or replace view trading_dashboard.v_live_bot_overview as
select c.bot_name,
       c.runtime_status status,
       c.environment,
       c.signal_source,
       ls.side last_signal_side,
       ls.signal_time_utc last_signal_at_utc,
       ld.decision last_decision,
       ld.reason last_decision_reason,
       ld.decided_at_utc last_decision_at_utc,
       coalesce(p.open_positions, 0)::int open_positions,
       coalesce(p.unrealized_pnl, 0) unrealized_pnl,
       coalesce(d.realized_pnl_today, 0) realized_pnl_today,
       coalesce(ls.strategy_version, 'unknown') strategy_version,
       h.last_seen_at_utc last_heartbeat_utc
from trading_dashboard.bot_configurations c
left join lateral (
    select side, signal_time_utc, strategy_version
    from trading_history.signals s
    where s.bot_name = c.bot_name
    order by signal_time_utc desc
    limit 1
) ls on true
left join lateral (
    select decision, reason, decided_at_utc
    from trading_history.strategy_decisions x
    where x.bot_name = c.bot_name
    order by decided_at_utc desc
    limit 1
) ld on true
left join lateral (
    select count(*) open_positions, 0::numeric unrealized_pnl
    from trading_history.positions x
    where x.bot_name = c.bot_name and x.closed_at_utc is null
) p on true
left join lateral (
    select coalesce(sum(realized_pnl), 0) realized_pnl_today
    from trading_history.positions x
    where x.bot_name = c.bot_name
      and x.closed_at_utc >= date_trunc('day', now() at time zone 'utc')
) d on true
left join lateral (
    select max(last_seen_at_utc) last_seen_at_utc
    from trading_dashboard.service_heartbeats sh
    where sh.component = c.bot_name
) h on true;

BEGIN;

ALTER TABLE trading_dashboard.service_heartbeats
    ADD COLUMN IF NOT EXISTS instance_id varchar(200),
    ADD COLUMN IF NOT EXISTS version varchar(50),
    ADD COLUMN IF NOT EXISTS environment varchar(30),
    ADD COLUMN IF NOT EXISTS started_at_utc timestamptz,
    ADD COLUMN IF NOT EXISTS stale_after_seconds integer NOT NULL DEFAULT 30;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'trading_dashboard'
          AND table_name = 'service_heartbeats'
          AND column_name = 'last_seen_utc'
    )
    AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'trading_dashboard'
          AND table_name = 'service_heartbeats'
          AND column_name = 'last_seen_at_utc'
    )
    THEN
        ALTER TABLE trading_dashboard.service_heartbeats
            RENAME COLUMN last_seen_utc TO last_seen_at_utc;
    END IF;
END
$$;

ALTER TABLE trading_dashboard.service_heartbeats
    ADD COLUMN IF NOT EXISTS last_seen_at_utc timestamptz NOT NULL DEFAULT now();

UPDATE trading_dashboard.service_heartbeats
SET instance_id = 'legacy'
WHERE instance_id IS NULL OR btrim(instance_id) = '';

ALTER TABLE trading_dashboard.service_heartbeats
    ALTER COLUMN instance_id SET DEFAULT 'legacy',
    ALTER COLUMN instance_id SET NOT NULL;

DO $$
DECLARE
    primary_key_name text;
BEGIN
    SELECT constraint_name
    INTO primary_key_name
    FROM information_schema.table_constraints
    WHERE table_schema = 'trading_dashboard'
      AND table_name = 'service_heartbeats'
      AND constraint_type = 'PRIMARY KEY'
    LIMIT 1;

    IF primary_key_name IS NOT NULL THEN
        EXECUTE format(
            'ALTER TABLE trading_dashboard.service_heartbeats DROP CONSTRAINT %I',
            primary_key_name
        );
    END IF;
END
$$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_service_heartbeats_component_instance
    ON trading_dashboard.service_heartbeats(component, instance_id);

COMMIT;

create schema if not exists trading_history;

create table if not exists trading_history.events
(
    event_id uuid primary key,
    event_type varchar(64) not null,
    occurred_at_utc timestamptz not null,
    environment varchar(32) not null,

    correlation_id varchar(128) null,
    bot_name varchar(64) null,
    strategy_version varchar(64) null,
    symbol varchar(32) null,
    position_id varchar(128) null,
    order_id varchar(128) null,
    side varchar(16) null,
    status varchar(64) null,

    price numeric(28, 12) null,
    quantity numeric(28, 12) null,
    realized_pnl numeric(28, 12) null,

    reason text null,
    data jsonb not null default '{}'::jsonb,
    raw_payload text null,

    created_at_utc timestamptz not null default now()
);

create index if not exists ix_historical_events_occurred_at
    on trading_history.events (occurred_at_utc desc);

create index if not exists ix_historical_events_type_occurred_at
    on trading_history.events (event_type, occurred_at_utc desc);

create index if not exists ix_historical_events_bot_occurred_at
    on trading_history.events (bot_name, occurred_at_utc desc)
    where bot_name is not null;

create index if not exists ix_historical_events_symbol_occurred_at
    on trading_history.events (symbol, occurred_at_utc desc)
    where symbol is not null;

create index if not exists ix_historical_events_correlation_id
    on trading_history.events (correlation_id)
    where correlation_id is not null;

create table if not exists trading_history.trade_summaries
(
    position_id varchar(128) primary key,
    bot_name varchar(64) not null,
    strategy_version varchar(64) not null,
    symbol varchar(32) not null,
    side varchar(16) not null,

    opened_at_utc timestamptz not null,
    closed_at_utc timestamptz not null,

    quantity numeric(28, 12) not null,
    entry_price numeric(28, 12) not null,
    exit_price numeric(28, 12) not null,

    gross_pnl numeric(28, 12) not null,
    commission numeric(28, 12) not null,
    net_pnl numeric(28, 12) not null,

    close_reason varchar(128) not null,
    environment varchar(32) not null,

    created_at_utc timestamptz not null default now(),
    updated_at_utc timestamptz not null default now()
);

create index if not exists ix_historical_trade_summaries_closed_at
    on trading_history.trade_summaries (closed_at_utc desc);

create index if not exists ix_historical_trade_summaries_bot_closed_at
    on trading_history.trade_summaries (bot_name, closed_at_utc desc);

create index if not exists ix_historical_trade_summaries_symbol_closed_at
    on trading_history.trade_summaries (symbol, closed_at_utc desc);