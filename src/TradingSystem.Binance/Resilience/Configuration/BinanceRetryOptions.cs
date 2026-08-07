namespace TradingSystem.Binance.Resilience.Configuration;

public sealed class BinanceRetryOptions
{
    public const string SectionName = "BinanceRetry";

    public bool Enabled { get; init; } = true;
    
    public int MaximumAttempts { get; init; } = 3;
    
    public int InitialDelayMilliseconds { get; init; } = 250;
    
    public int MaximumDelayMilliseconds { get; init; } = 2_000;
    
    public double BackoffMultiplier { get; init; } = 2;
    
    public bool UseJitter { get; init; } = true;
}
