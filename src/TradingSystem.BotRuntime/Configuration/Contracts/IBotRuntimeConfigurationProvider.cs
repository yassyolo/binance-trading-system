namespace TradingSystem.BotRuntime.Configuration.Contracts;

public interface IBotRuntimeConfigurationProvider
{
    Task<BotRuntimeConfiguration?> GetAsync(string botName, CancellationToken ct);
    BotRuntimeConfiguration? GetCurrent(string botName);
    void Set(BotRuntimeConfiguration configuration);
    void Invalidate(string botName);
}
