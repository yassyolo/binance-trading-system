using StrategyService.Bots.Common.TpOnlyGrid;
using TradingSystem.Binance.Startup;

namespace StrategyService.Bots.Bot8012.Configuration;

public sealed class Bot8012Options : IBinanceTradingConfiguration, ITpOnlyGridBotOptions
{ 
    public const string SectionName = "Bots:Bot8012"; 
    
    public string BotName { get; set; } = "BOT8012"; 
  
    public string StrategyVersion { get; set; } = "1.0.0"; 
    
    public string Symbol { get; set; } = "BTCUSDC"; 
    
    public decimal Quantity { get; set; } = .002m; 
    
    public int Leverage { get; set; } = 50; 
    
    public decimal PriceDistance { get; set; } = 400; 
    
    public decimal ProfitDistance { get; set; } = 200; 
    
    public int OrderSideLimit { get; set; } = 2; 
    
    public int CooldownSeconds { get; set; } = 180; 
    
    public bool EnableLong { get; set; } = true; 
    
    public bool EnableShort { get; set; } = true; 
    
    public bool EnableHealing { get; set; } = true; 
    
    public Bot8012SignalRules SignalRules { get; set; } = new(); 
}
