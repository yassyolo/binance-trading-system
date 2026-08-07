using TradingSystem.ReplayEngine.Accumulator;

namespace TradingSystem.ReplayEngine.Models;

public sealed record ReplayContext(Guid ReplayId, DateTime VirtualTimeUtc, ReplayAccumulator State);
