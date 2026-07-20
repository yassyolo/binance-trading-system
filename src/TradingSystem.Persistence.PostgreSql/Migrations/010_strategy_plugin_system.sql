CREATE TABLE IF NOT EXISTS trading_dashboard.strategy_plugins (
    plugin_id varchar(128) PRIMARY KEY, 
    display_name varchar(128) NOT NULL, 
    strategy_name varchar(128) NOT NULL, 
    version varchar(32) NOT NULL, 
    assembly_name varchar(256) NOT NULL, 
    is_builtin boolean NOT NULL DEFAULT false, 
    enabled boolean NOT NULL DEFAULT true, 
    supported_symbols jsonb NOT NULL DEFAULT '[]'::jsonb, 
    description text NULL, 
    discovered_at_utc timestamptz NOT NULL DEFAULT now(), 
    updated_at_utc timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_strategy_plugins_enabled
    ON trading_dashboard.strategy_plugins(enabled,  plugin_id);

ALTER TABLE trading_dashboard.bot_configurations
    ADD COLUMN IF NOT EXISTS strategy_plugin_version varchar(32) NULL;

COMMENT ON COLUMN trading_dashboard.bot_configurations.strategy_type IS
    'Strategy plugin id. Existing BOT8011..BOT8016 values remain valid aliases for built-in strategies.';
