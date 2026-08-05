/*begin;

alter table trading_paper.positions
    add column if not exists signal_id text null;

alter table trading_paper.positions
    add column if not exists strategy_version text not null default 'unknown';

create index if not exists ix_trading_paper_positions_signal_id
    on trading_paper.positions(signal_id)
    where signal_id is not null;

-- Existing history rows cannot always be backfilled safely because older paper rows did not
-- persist the originating signal. New positions will be linked automatically after deployment.

commit;
*/