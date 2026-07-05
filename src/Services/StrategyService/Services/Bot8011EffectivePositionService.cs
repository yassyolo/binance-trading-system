using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService.Services;

public sealed class Bot8011EffectivePositionService(
        IOptions<Bot8011Options> options,
        IPositionStore positionStore,
        IBinanceFuturesOrderClient orders)
{
    private readonly Bot8011Options options = options.Value;

    public async Task<IReadOnlyCollection<BotPosition>> GetEffectiveActiveAsync(
        CancellationToken cancellationToken)
    {
        var redisPositions = await positionStore.GetAllAsync(
            options.BotName,
            cancellationToken);

        var openOrders = await orders.GetOpenOrdersAsync(
            options.Symbol,
            cancellationToken);

        var openAlgoOrders = await orders.GetOpenAlgoOrdersAsync(
            options.Symbol,
            cancellationToken);

        var shortBot = GetShortBot(options.BotName);

        var activeShortIds = new HashSet<string>();

        foreach (var tp in openOrders.Where(x =>
                     x.Type == "LIMIT" &&
                     x.ClientOrderId.StartsWith($"{shortBot}_TP_", StringComparison.OrdinalIgnoreCase)))
        {
            if (TryParseClientId(tp.ClientOrderId, out _, out var shortId))
                activeShortIds.Add(shortId);
        }

        foreach (var stop3 in openAlgoOrders.Where(x =>
                     IsStop3ClientId(shortBot, x.ClientAlgoId)))
        {
            if (TryParseClientId(stop3.ClientAlgoId, out _, out var shortId))
                activeShortIds.Add(shortId);
        }

        foreach (var position in redisPositions)
        {
            if (position.Closed)
                continue;

            var isTransitional =
                position.TpExecuted &&
                position.RemainingQuantity > 0 &&
                position.Stop3Pending &&
                position.ProtectiveActive;

            if (isTransitional)
                activeShortIds.Add(position.ShortId);
        }

        return redisPositions
            .Where(x => activeShortIds.Contains(x.ShortId))
            .ToList();
    }

    public async Task<int> CountBySideAsync(
        PositionSide side,
        CancellationToken cancellationToken)
    {
        var active = await GetEffectiveActiveAsync(cancellationToken);

        return active.Count(x => x.Side == side);
    }

    public async Task<IReadOnlyCollection<BotPosition>> GetOppositeAsync(
        PositionSide side,
        CancellationToken cancellationToken)
    {
        var opposite = side == PositionSide.Long
            ? PositionSide.Short
            : PositionSide.Long;

        var active = await GetEffectiveActiveAsync(cancellationToken);

        return active
            .Where(x => x.Side == opposite)
            .ToList();
    }

    private static bool IsStop3ClientId(string shortBot, string clientId)
        => clientId.StartsWith($"{shortBot}_S3_", StringComparison.OrdinalIgnoreCase) ||
           clientId.StartsWith($"{shortBot}_STOP3_", StringComparison.OrdinalIgnoreCase);

    private static string GetShortBot(string botName)
        => botName.Length > 8 ? botName[..8] : botName;

    private static bool TryParseClientId(
        string? clientOrderId,
        out string type,
        out string shortId)
    {
        type = string.Empty;
        shortId = string.Empty;

        if (string.IsNullOrWhiteSpace(clientOrderId))
            return false;

        var parts = clientOrderId.Split('_', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
            return false;

        type = parts[1].ToUpperInvariant();
        shortId = parts[2];

        return true;
    }
}