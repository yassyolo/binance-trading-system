namespace TradingSystem.Backtesting.Bots.Configuration;

public sealed record Bot8012BacktestOptions
{
    public string BotName { get; init; } = "BOT8012";
    
    public string StrategyVersion { get; init; } = "1.0.0";
    
    public string Symbol { get; init; } = "BTCUSDC";
    
    public decimal InitialBalance { get; init; } = 10_000m;
    
    public decimal Quantity { get; init; } = 0.002m;
    
    public int Leverage { get; init; } = 50;
    
    public decimal PriceDistance { get; init; } = 400m;
    
    public decimal ProfitDistance { get; init; } = 200m;
   
    public int OrderSideLimit { get; init; } = 2;
   
    public int CooldownSeconds { get; init; } = 180;
    
    public bool EnableLong { get; init; } = true;
    
    public bool EnableShort { get; init; } = true;
    
    public decimal EntryFeeRate { get; init; } = 0.0004m;
   
    public decimal ExitFeeRate { get; init; } = 0.0004m;
    
    public decimal SlippageBasisPoints { get; init; } = 1m;
    
    public decimal TickSize { get; init; } = 0.1m;
    
    public bool ForceCloseAtEnd { get; init; } = true;
}
