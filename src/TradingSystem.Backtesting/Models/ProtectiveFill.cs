using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Models;

public sealed record ProtectiveFill(ExitReason Reason, decimal Price);

