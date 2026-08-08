using TradingSystem.Dashboard.Contracts.Models.Enums;

namespace TradingSystem.Dashboard.Contracts.Models.Bots;

public sealed record BotConfigurationDto(
    string BotName, 
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
    long Version,
    DateTime UpdatedAtUtc,
    string UpdatedBy,
    bool RestartRequired);

