using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;

namespace StrategyService.Positions;

public sealed class Bot8015ActivePositionProvider(
    IOptions<Bot8015Options> options,
    IBinanceFuturesOrderClient orders,
    IPositionStore positionStore,
    ILogger<Bot8015ActivePositionProvider> logger)
    : IBotActivePositionProvider
{
    private readonly Bot8015Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, ActivePositionView>();

        var openOrders = await orders.GetOpenOrdersAsync(symbol, cancellationToken);

        foreach (var order in openOrders.Where(x =>
                     x.ClientOrderId.StartsWith($"{_options.BotName}_TP_", StringComparison.OrdinalIgnoreCase) &&
                     x.Type.Equals("LIMIT", StringComparison.OrdinalIgnoreCase) &&
                     x.Price > 0))
        {
            var shortId = ExtractShortId(order.ClientOrderId);
            if (string.IsNullOrWhiteSpace(shortId))
                continue;

            result[shortId] = new ActivePositionView
            {
                ShortId = shortId,
                BotName = _options.BotName,
                Symbol = symbol,
                Side = ParseSide(order.PositionSide),
                TpPrice = order.Price,
                CreatedAtUtc = order.UpdateTimeUtc
            };
        }

        var algoOrders = await orders.GetOpenAlgoOrdersAsync(symbol, cancellationToken);

        foreach (var algo in algoOrders.Where(x =>
                     x.ClientAlgoId.StartsWith($"{_options.BotName}_STOP3_", StringComparison.OrdinalIgnoreCase)))
        {
            var shortId = ExtractShortId(algo.ClientAlgoId);
            if (string.IsNullOrWhiteSpace(shortId) || result.ContainsKey(shortId))
                continue;

            result[shortId] = new ActivePositionView
            {
                ShortId = shortId,
                BotName = _options.BotName,
                Symbol = symbol,
                Side = ParseSide(algo.PositionSide),
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        var redisPositions = await positionStore.GetAllAsync(_options.BotName, cancellationToken);

        foreach (var position in redisPositions)
        {
            if (result.ContainsKey(position.ShortId))
                continue;

            if (position.TpExecuted &&
                position.RemainingQuantity > 0 &&
                position.Stop3Pending &&
                position.ProtectiveActive)
            {
                result[position.ShortId] = new ActivePositionView
                {
                    ShortId = position.ShortId,
                    BotName = _options.BotName,
                    Symbol = position.Symbol,
                    Side = position.Side,
                    CreatedAtUtc = position.UpdatedAtUtc ?? position.CreatedAtUtc
                };
            }
        }

        logger.LogInformation(
            "BOT8015 effective active positions loaded. Count={Count}",
            result.Count);

        return result.Values
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();
    }

    private static string ExtractShortId(string clientId)
    {
        var parts = clientId.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? parts[2] : string.Empty;
    }

    private static PositionSide ParseSide(string value)
        => value.Equals("SHORT", StringComparison.OrdinalIgnoreCase)
            ? PositionSide.Short
            : PositionSide.Long;
}