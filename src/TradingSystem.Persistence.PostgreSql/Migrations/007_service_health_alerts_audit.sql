create schema if not exists trading_dashboard;

alter table trading_dashboard.service_heartbeats
 add column if not exists instance_id varchar(200) not null default 'legacy', 
 add column if not exists version varchar(50), 
 add column if not exists environment varchar(30), 
 add column if not exists started_at_utc timestamptz, 
 add column if not exists stale_after_seconds int not null default 30;

do $$ begin
 if exists(select 1 from pg_constraint where conname = 'service_heartbeats_pkey') then alter table trading_dashboard.service_heartbeats drop constraint service_heartbeats_pkey; end if;
exception when undefined_object then null; end $$;
create unique index if not exists ux_service_heartbeats_component_instance on trading_dashboard.service_heartbeats(component, instance_id);

alter table trading_dashboard.alerts
 add column if not exists deduplication_key varchar(500), 
 add column if not exists resolved boolean not null default false, 
 add column if not exists resolved_at_utc timestamptz, 
 add column if not exists last_seen_at_utc timestamptz not null default now(), 
 add column if not exists occurrence_count bigint not null default 1;
update trading_dashboard.alerts set deduplication_key = 'legacy:' || alert_id where deduplication_key is null;
alter table trading_dashboard.alerts alter column deduplication_key set not null;
create unique index if not exists ux_alerts_deduplication_key on trading_dashboard.alerts(deduplication_key);
create index if not exists ix_alerts_active_severity on trading_dashboard.alerts(resolved, severity, last_seen_at_utc desc);

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
