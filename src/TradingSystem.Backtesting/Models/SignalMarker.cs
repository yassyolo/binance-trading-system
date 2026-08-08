using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Models;

public sealed record SignalMarker(DateTime TimeUtc, TradeSide Side, decimal Price, bool Executed, string Reason);

