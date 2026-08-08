using TradingSystem.Dashboard.Contracts.Models.Enums;

namespace TradingSystem.Dashboard.Contracts.Models.Bots;

public sealed record UpdateBotConfigurationRequest(
    long ExpectedVersion, 
    string StrategyType, 
    string Symbol, 
    TradingEnvironment Environment, 
    string SignalSource,
    bool EnableLong, 
    bool EnableShort,
    decimal Quantity, 
    int Leverage, 
    decimal? PriceDistance, 
    decimal? ProfitDistance, 
    int? OrderSideLimit, 
    int CooldownSeconds, 
    string Reason);

