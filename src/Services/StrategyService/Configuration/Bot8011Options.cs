namespace StrategyService.Configuration;

public sealed class Bot8011Options
{
    public string BotName { get; set; } = "BOT8011";

    public string Symbol { get; set; } = "BTCUSDC";

    public decimal Quantity { get; set; } = 0.01m;

    public int Leverage { get; set; } = 10;

    public decimal TakeProfitPercent { get; set; } = 0.12m;

    public decimal StopLossPercent { get; set; } = 0.20m;

    public decimal Stop3Percent { get; set; } = 0.10m;

    public int CooldownSeconds { get; set; } = 5;

    public bool EnableLong { get; set; } = true;

    public bool EnableShort { get; set; } = true;

    public int OrderSideLimit { get; set; } = 1;


    public decimal Stop3EntryOffset { get; set; }

    public decimal Stop3TrailingDistance { get; set; }
    public int Stop3TrailingIntervalSeconds { get; set; } = 5;
}