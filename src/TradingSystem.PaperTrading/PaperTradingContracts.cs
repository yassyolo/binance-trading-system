using TradingSystem.Domain.Enums;

namespace TradingSystem.PaperTrading;

public enum PaperOrderStatus { Open  =  1,  Filled  =  2,  Cancelled  =  3,  Rejected  =  4 }
public enum PaperPositionStatus { Open  =  1,  Closed  =  2 }

public sealed record PaperTradingPosition(
    Guid PositionId, 
    string ShortId, 
    string BotName, 
    string Symbol, 
    PositionSide Side, 
    decimal Quantity, 
    decimal EntryPrice, 
    decimal TakeProfitPrice, 
    decimal StopLossPrice, 
    decimal EntryFee, 
    decimal? ExitPrice, 
    decimal? ExitFee, 
    decimal? RealizedPnl, 
    PaperPositionStatus Status, 
    string Source, 
    DateTime OpenedAtUtc, 
    DateTime? ClosedAtUtc, 
    string? CloseReason, 
    long Version);

public sealed record PaperTradingAccount(
    decimal InitialBalance, 
    decimal RealizedPnl, 
    decimal Fees, 
    decimal Equity, 
    int OpenPositions, 
    int ClosedPositions, 
    DateTime CalculatedAtUtc);

public interface IPaperTradingStore
{
    Task CreateAsync(PaperTradingPosition position,  CancellationToken cancellationToken);
    Task<PaperTradingPosition?> GetAsync(string botName,  string shortId,  CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaperTradingPosition>> GetOpenAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PaperTradingPosition>> QueryAsync(string? botName,  string? symbol,  PaperPositionStatus? status,  int skip,  int take,  CancellationToken cancellationToken);
    Task<bool> TryCloseAsync(Guid positionId,  long expectedVersion,  decimal exitPrice,  decimal exitFee,  decimal realizedPnl,  string reason,  DateTime closedAtUtc,  CancellationToken cancellationToken);
    Task<PaperTradingAccount> GetAccountAsync(decimal initialBalance,  CancellationToken cancellationToken);
    Task ResetAsync(string actor,  CancellationToken cancellationToken);
}
