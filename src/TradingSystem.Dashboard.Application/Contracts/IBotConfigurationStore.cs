using TradingSystem.Dashboard.Contracts;

namespace TradingSystem.Dashboard.Application.Contracts;

public interface IBotConfigurationStore
{
    Task<IReadOnlyCollection<BotConfigurationDto>> GetAllAsync(CancellationToken ct);
    
    Task<BotConfigurationDto?> GetAsync(string botName, CancellationToken ct);
    
    Task<BotConfigurationDto> UpdateAsync(string botName, UpdateBotConfigurationRequest request, string user, CancellationToken ct);
}