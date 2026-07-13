using TradingSystem.Binance.Startup;

namespace StrategyService.Configuration;

public sealed class Bot8015Options : IBinanceTradingConfiguration
{
    public const string SectionName = "Bots:Bot8015";

    public string BotName { get; set; } = "BOT8015";
    public string StrategyVersion { get; set; } = "1.0.0";
    public string Symbol { get; set; } = "BTCUSDC";

    public decimal Quantity { get; set; } = 0.002m;
    public int Leverage { get; set; } = 50;

    public decimal InitialStopLoss { get; set; } = 300m;
    public decimal TpPercent { get; set; } = 0.12m;

    public int OrderSideLimit { get; set; } = 1;
    public int CooldownSeconds { get; set; } = 180;

    public decimal Stop3TrailingStep { get; set; } = 400m;
    public decimal Stop3TrailingBuffer { get; set; } = 50m;
    public decimal Stop3EntryOffset { get; set; } = 0m;

    public bool EnableLong { get; set; } = true;
    public bool EnableShort { get; set; } = true;
    public bool EnableHealing { get; set; } = true;
    public bool EnableTrailing { get; set; } = true;

    public int HealingIntervalSeconds { get; set; } = 10;
    public int TrailingCheckIntervalSeconds { get; set; } = 5;
    public string KlineInterval { get; set; } = "1m";
}
