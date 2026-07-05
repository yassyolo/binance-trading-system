using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Healing;

namespace StrategyService.Services.Healing;

public sealed class Bot8012HealingService : IBotHealingService
{
    private readonly Bot8012Options _options;
    private readonly IPositionStore _positionStore;
    private readonly ILogger<Bot8012HealingService> _logger;

    public Bot8012HealingService(
        IOptions<Bot8012Options> options,
        IPositionStore positionStore,
        ILogger<Bot8012HealingService> logger)
    {
        _options = options.Value;
        _positionStore = positionStore;
        _logger = logger;
    }

    public string BotName => _options.BotName;

    public async Task HealAsync(
        HealingSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (!snapshot.Type.Equals("healing_snapshot", StringComparison.OrdinalIgnoreCase))
            return;

        if (!snapshot.Symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase))
            return;

        var activeIds = snapshot.ActiveClientIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var positions = await _positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        var healed = 0;

        foreach (var position in positions.Where(x => !x.Closed))
        {
            if (string.IsNullOrWhiteSpace(position.TpClientId))
                continue;

            if (activeIds.Contains(position.TpClientId))
                continue;

            await _positionStore.DeleteAsync(
                _options.BotName,
                position.ShortId,
                cancellationToken);

            healed++;

            _logger.LogWarning(
                "BOT8012 healing removed stale position. Position={ShortId}",
                position.ShortId);
        }

        if (healed > 0)
        {
            _logger.LogInformation(
                "BOT8012 healing completed. Removed={Count}",
                healed);
        }
    }
}