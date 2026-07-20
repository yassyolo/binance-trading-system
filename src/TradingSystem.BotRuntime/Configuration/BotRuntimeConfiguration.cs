namespace TradingSystem.BotRuntime.Configuration;

public sealed record BotRuntimeConfiguration(
    string BotName, 
    string StrategyType, 
    string Symbol, 
    string Environment, 
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
    bool RestartRequired);

public interface IBotRuntimeConfigurationStore
{
    Task<BotRuntimeConfiguration?> GetAsync(string botName,  CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BotRuntimeConfiguration>> GetChangedSinceAsync(DateTime changedSinceUtc,  CancellationToken cancellationToken);
}

public interface IBotRuntimeConfigurationProvider
{
    Task<BotRuntimeConfiguration?> GetAsync(string botName,  CancellationToken cancellationToken);
    BotRuntimeConfiguration? GetCurrent(string botName);
    void Set(BotRuntimeConfiguration configuration);
    void Invalidate(string botName);
}
