namespace StrategyService.Bots.Common.TpOnlyGrid;

public interface ITpOnlyGridBotOptions
{
    string BotName { get; }
    
    string StrategyVersion { get; }
    
    string Symbol { get; }
    
    decimal Quantity { get; }
    
    int Leverage { get; }
    
    decimal PriceDistance { get; }
    
    decimal ProfitDistance { get; }
    
    int OrderSideLimit { get; }
    
    int CooldownSeconds { get; }
    
    bool EnableLong { get; }
    
    bool EnableShort { get; }
    
    bool EnableHealing { get; }
}
