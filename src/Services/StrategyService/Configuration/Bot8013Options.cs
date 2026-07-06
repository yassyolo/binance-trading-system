using TradingSystem.Binance.Startup;

namespace StrategyService.Configuration;

public sealed class Bot8013Options : IBinanceTradingConfiguration
{
    public string BotName { get; set; } = "BOT8013";
    public string Symbol { get; set; } = "BTCUSDC";
    public decimal Quantity { get; set; } = 0.002m;
    public int Leverage { get; set; } = 50;

    public decimal PriceDistance { get; set; } = 400;
    public decimal ProfitDistance { get; set; } = 200;

    public int OrderSideLimit { get; set; } = 2;
    public int CooldownSeconds { get; set; } = 180;

    public bool EnableLong { get; set; } = true;
    public bool EnableShort { get; set; } = true;

    public bool EnableHealing { get; set; } = true;
}