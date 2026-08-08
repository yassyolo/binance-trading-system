using TradingSystem.Dashboard.Contracts.Models.Bots;

namespace TradingSystem.Dashboard.Application.Contracts;

public interface IBotCommandStore
{
    Task<BotCommandDto> EnqueueAsync(string botName, BotCommandRequest request, string user, CancellationToken ct);
    
    Task<IReadOnlyCollection<BotCommandDto>> GetAsync(string? botName, int take, CancellationToken ct);
}
