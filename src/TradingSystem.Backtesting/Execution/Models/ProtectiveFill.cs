using TradingSystem.Backtesting.Models.Enums;

namespace TradingSystem.Backtesting.Execution.Models;

public sealed record ProtectiveFill(ExitReason Reason, decimal Price);

