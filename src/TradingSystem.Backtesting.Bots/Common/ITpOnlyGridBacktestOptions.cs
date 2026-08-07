namespace TradingSystem.Backtesting.Bots.Common;

public interface ITpOnlyGridBacktestOptions
{
    string BotName { get; }
    string Symbol { get; }
    decimal InitialBalance { get; }
    decimal Quantity { get; }
    int Leverage { get; }
    decimal PriceDistance { get; }
    decimal ProfitDistance { get; }
    int OrderSideLimit { get; }
    int CooldownSeconds { get; }
    bool EnableLong { get; }
    bool EnableShort { get; }
    decimal EntryFeeRate { get; }
    decimal ExitFeeRate { get; }
    decimal SlippageBasisPoints { get; }
    decimal TickSize { get; }
    bool ForceCloseAtEnd { get; }
}
