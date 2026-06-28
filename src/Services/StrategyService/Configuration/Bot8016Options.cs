namespace StrategyService.Configuration;

public sealed class Bot8016Options
{
    public string BotName { get; set; } = "BOT8016";
    public string Symbol { get; set; } = "BTCUSDC";
    public decimal Quantity { get; set; } = 0.002m;
    public int Leverage { get; set; } = 50;
    public decimal InitialStopLoss { get; set; } = 300;
    public decimal TakeProfitPercent { get; set; } = 0.12m;
    public int PositionSideLimit { get; set; } = 1;
    public bool UseMa200Filter { get; set; } = true;
    public string EntryTimeframe { get; set; } = "5m";
    public string ExitTimeframe { get; set; } = "1m";
}