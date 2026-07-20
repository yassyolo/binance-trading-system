create table if not exists trading_dashboard.api_idempotency_keys
(
    idempotency_key varchar(128) not null, 
    actor varchar(256) not null, 
    method varchar(16) not null, 
    path varchar(512) not null, 
    request_hash char(64) not null, 
    status varchar(24) not null, 
    response_status_code integer, 
    response_content_type varchar(256), 
    response_body bytea, 
    created_at_utc timestamptz not null default now(), 
    completed_at_utc timestamptz, 
    expires_at_utc timestamptz not null, 
    constraint pk_api_idempotency_keys primary key (idempotency_key,  actor), 
    constraint ck_api_idempotency_status check (status in ('Processing',  'Completed'))
);

create index if not exists ix_api_idempotency_expiry
    on trading_dashboard.api_idempotency_keys(expires_at_utc);

create index if not exists ix_api_idempotency_processing
    on trading_dashboard.api_idempotency_keys(status,  created_at_utc)
    where status  =  'Processing';

-- Safe cleanup can be executed periodically by an operational job.
delete from trading_dashboard.api_idempotency_keys
where expires_at_utc < now();
