using TradingSystem.Binance.Startup;

namespace StrategyService.Configuration;

public sealed class Bot8014Options : IBinanceTradingConfiguration
{
    public const string SectionName = "Bots:Bot8014";

    public string BotName { get; set; } = "BOT8014";
    public string StrategyVersion { get; set; } = "1.0.0";
    public string Symbol { get; set; } = "BTCUSDC";

    public decimal Quantity { get; set; } = 0.002m;
    public int Leverage { get; set; } = 50;

    public decimal PriceDistance { get; set; } = 400m;
    public decimal ProfitDistance { get; set; } = 200m;

    public int OrderSideLimit { get; set; } = 2;
    public int CooldownSeconds { get; set; } = 180;

    public bool EnableLong { get; set; } = true;
    public bool EnableShort { get; set; } = true;

    public bool EnableHealing { get; set; } = true;
    public int HealingIntervalSeconds { get; set; } = 10;
}
