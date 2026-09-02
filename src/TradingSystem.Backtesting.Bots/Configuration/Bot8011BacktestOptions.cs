using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Bots.Configuration;

public sealed record Bot8011BacktestOptions
{
    public string BotName { get; init; } = "BOT8011";
   
    public string Symbol { get; init; } = "BTCUSDC";
    
    public decimal Quantity { get; init; } = 0.002m;
    
    public int Leverage { get; init; } = 50;
    
    public decimal TakeProfitPercent { get; init; } = 0.12m;
    
    public decimal InitialStopLossDistance { get; init; } = 300m;
    
    public decimal TakeProfitCloseFraction { get; init; } = 0.50m;
   
    public int CooldownSeconds { get; init; } = 180;
    
    public bool EnableLong { get; init; } = true;
    
    public bool EnableShort { get; init; } = true;
    
    public decimal Stop3EntryOffset { get; init; }
   
    public decimal Stop3TrailingStep { get; init; } = 400m;
    
    public decimal Stop3TrailingBuffer { get; init; } = 50m;
    
    public decimal TakerFeeRate { get; init; } = 0.0004m;
    
    public decimal SlippageBasisPoints { get; init; } = 1m;
    
    public decimal MinimumQuantity { get; init; } = 0.001m;
    
    public decimal QuantityStep { get; init; } = 0.001m;
    
    public decimal MinimumNotional { get; init; } = 5m;
    
    public decimal TickSize { get; init; } = 0.1m;
    
    public bool EnterOnNextCandleOpen { get; init; } = true;
    
    public IntrabarConflictPolicy ConflictPolicy { get; init; } = IntrabarConflictPolicy.WorstCase;
}
