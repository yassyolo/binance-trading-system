using TradingSystem.Binance.Startup;

namespace StrategyService.Configuration;

public sealed class Bot8011Options : IBinanceTradingConfiguration
{
    public const string SectionName = "Bots:Bot8011";
    public string StrategyVersion { get; set; } = "1.0.0";

    public string BotName { get; set; } = "BOT8011";
    public string Symbol { get; set; } = "BTCUSDC";
    public decimal Quantity { get; set; } = 0.002m;
    public int Leverage { get; set; } = 50;

    // Python parity: fixed price distance, not percentage.
    public decimal InitialStopLossDistance { get; set; } = 300m;
    public decimal TakeProfitPercent { get; set; } = 0.12m;

    public int CooldownSeconds { get; set; } = 180;
    public bool EnableLong { get; set; } = true;
    public bool EnableShort { get; set; } = true;
    public int OrderSideLimit { get; set; } = 1;

    public decimal Stop3EntryOffset { get; set; } = 0m;
    public decimal Stop3TrailingStep { get; set; } = 400m;
    public decimal Stop3TrailingBuffer { get; set; } = 50m;

    public string TrailingInterval { get; set; } = "1m";
    public int TrailingPriceMaxAgeSeconds { get; set; } = 90;
    public int TrailingFallbackIntervalSeconds { get; set; } = 5;
}
