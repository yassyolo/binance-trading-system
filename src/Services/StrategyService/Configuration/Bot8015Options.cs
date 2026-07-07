using TradingSystem.Binance.Startup;

namespace StrategyService.Configuration;

public sealed class Bot8015Options : IBinanceTradingConfiguration
{
    public string BotName { get; set; } = "BOT8015";
    public string Symbol { get; set; } = "BTCUSDC";
    public decimal Quantity { get; set; } = 0.002m;
    public int Leverage { get; set; } = 50;

    public decimal InitialStopLoss { get; set; } = 300;
    public decimal TpPercent { get; set; } = 0.12m;

    public int OrderSideLimit { get; set; } = 1;
    public int CooldownSeconds { get; set; } = 180;

    public decimal Stop3TrailingStep { get; set; } = 400;
    public decimal Stop3TrailingBuffer { get; set; } = 50;
    public decimal Stop3EntryOffset { get; set; } = 0;

    public bool EnableLong { get; set; } = true;
    public bool EnableShort { get; set; } = true;
    public bool EnableHealing { get; set; } = true;
    public bool EnableTrailing { get; set; } = true;
}