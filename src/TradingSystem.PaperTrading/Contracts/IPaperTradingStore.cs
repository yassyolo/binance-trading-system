using TradingSystem.PaperTrading.Models;
using TradingSystem.PaperTrading.Models.Enums;

namespace TradingSystem.PaperTrading.Contracts;

public interface IPaperTradingStore
{
    Task CreateAsync(PaperTradingPosition position, CancellationToken ct);
    
    Task<PaperTradingPosition?> GetAsync(string botName, string shortId, CancellationToken ct);
    
    Task<IReadOnlyCollection<PaperTradingPosition>> GetOpenAsync(CancellationToken ct);
    
    Task<IReadOnlyCollection<PaperTradingPosition>> QueryAsync(
        string? botName, 
        string? symbol, 
        PaperPositionStatus? status, 
        int skip, 
        int take, 
        CancellationToken ct);
    
    Task<bool> TryCloseAsync(
        Guid positionId, 
        long expectedVersion, 
        decimal exitPrice, 
        decimal exitFee, 
        decimal realizedPnl, 
        string reason, 
        DateTime closedAtUtc, 
        CancellationToken ct);
    
    Task<PaperTradingAccount> GetAccountAsync(decimal initialBalance, CancellationToken ct);
    
    Task ResetAsync(string actor, CancellationToken ct);
}
