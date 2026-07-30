/*create schema if not exists trading_dashboard;

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
*/