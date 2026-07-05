using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Binance.Orders.Contracts;

namespace StrategyService.Positions;

public sealed class Bot8011ActivePositionProvider(
    IOptions<Bot8011Options> options,
    IPositionStore positionStore,
    IBinanceFuturesOrderClient orders,
    ILogger<Bot8011ActivePositionProvider> logger)
    : IBotActivePositionProvider
{
    private readonly Bot8011Options options = options.Value;
    public string BotName => options.BotName;

    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
    string symbol,
    CancellationToken cancellationToken)
    {
        var redisPositions = await positionStore.GetAllAsync(
            options.BotName,
            cancellationToken);

        var openOrders = await orders.GetOpenOrdersAsync(
            symbol,
            cancellationToken);

        var openAlgoOrders = await orders.GetOpenAlgoOrdersAsync(
            symbol,
            cancellationToken);

        var shortBot = GetShortBot(options.BotName);
        var activeShortIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tp in openOrders.Where(x =>
                     x.Type.Equals("LIMIT", StringComparison.OrdinalIgnoreCase) &&
                     !string.IsNullOrWhiteSpace(x.ClientOrderId) &&
                     x.ClientOrderId.StartsWith($"{shortBot}_TP_", StringComparison.OrdinalIgnoreCase)))
        {
            if (TryParseClientId(tp.ClientOrderId, out var shortId))
                activeShortIds.Add(shortId);
        }

        foreach (var algo in openAlgoOrders.Where(x =>
                     !string.IsNullOrWhiteSpace(x.ClientAlgoId) &&
                     IsBotStop3(shortBot, x.ClientAlgoId)))
        {
            if (TryParseClientId(algo.ClientAlgoId, out var shortId))
                activeShortIds.Add(shortId);
        }

        foreach (var position in redisPositions.Where(x => !x.Closed))
        {
            var transitional =
                position.TpExecuted &&
                position.RemainingQuantity > 0 &&
                position.Stop3Pending &&
                position.ProtectiveActive;

            if (transitional)
                activeShortIds.Add(position.ShortId);
        }

        var result = redisPositions
            .Where(x =>
                !x.Closed &&
                x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase) &&
                activeShortIds.Contains(x.ShortId))
            .Select(x => new ActivePositionView
            {
                ShortId = x.ShortId,
                BotName = x.BotName,
                Symbol = x.Symbol,
                Side = x.Side,
                Quantity = x.Quantity,
                RemainingQuantity = x.RemainingQuantity,
                EntryPrice = x.EntryPrice,
                CreatedAtUtc = x.ParentFilledAtUtc ?? x.CreatedAtUtc
            })
            .ToList();

        logger.LogDebug(
            "BOT8011 active positions resolved. Symbol={Symbol}, Count={Count}",
            symbol,
            result.Count);

        return result;
    }

    private static bool IsBotStop3(string shortBot, string clientAlgoId)
        => clientAlgoId.StartsWith($"{shortBot}_S3_", StringComparison.OrdinalIgnoreCase) ||
           clientAlgoId.StartsWith($"{shortBot}_STOP3_", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseClientId(string? clientId, out string shortId)
    {
        shortId = string.Empty;

        if (string.IsNullOrWhiteSpace(clientId))
            return false;

        var parts = clientId.Split('_', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
            return false;

        shortId = parts[2];

        return shortId.Length > 0;
    }

    private static string GetShortBot(string botName)
        => botName.Length > 8 ? botName[..8] : botName;
}