using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
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
        EnsureSupportedSymbol(symbol);

        var result = new Dictionary<string, ActivePositionView>(
            StringComparer.OrdinalIgnoreCase);

        var openOrders = await orders.GetOpenOrdersAsync(
            symbol,
            cancellationToken);

        foreach (var order in openOrders)
        {
            if (!TryExtractShortId(
                    order.ClientOrderId,
                    $"{_options.BotName}_TP_",
                    out var shortId))
            {
                continue;
            }

            if (!order.Type.Equals("LIMIT", StringComparison.OrdinalIgnoreCase)
                || order.Price <= 0
                || !TryParseSide(order.PositionSide, out var side))
            {
                continue;
            }

            result[shortId] = new ActivePositionView
            {
                ShortId = shortId,
                BotName = _options.BotName,
                Symbol = symbol,
                Side = side,
                TpPrice = order.Price,
                CreatedAtUtc = order.UpdateTimeUtc
            };
        }

        var algoOrders = await orders.GetOpenAlgoOrdersAsync(
            symbol,
            cancellationToken);

        foreach (var algo in algoOrders)
        {
            if (!TryExtractShortId(
                    algo.ClientAlgoId,
                    $"{_options.BotName}_STOP3_",
                    out var shortId))
            {
                continue;
            }

            if (result.ContainsKey(shortId)
                || !TryParseSide(algo.PositionSide, out var side))
            {
                continue;
            }

            result[shortId] = new ActivePositionView
            {
                ShortId = shortId,
                BotName = _options.BotName,
                Symbol = symbol,
                Side = side,
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        var storedPositions = await positionStore.GetAllAsync(
            _options.BotName,
            cancellationToken);

        foreach (var position in storedPositions)
        {
            if (position.Closed
                || result.ContainsKey(position.ShortId)
                || !position.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isTransitioningToStop3 =
                position.TpExecuted
                && position.RemainingQuantity > 0
                && position.Stop3Pending
                && position.ProtectiveActive;

            if (!isTransitioningToStop3)
                continue;

            result[position.ShortId] = new ActivePositionView
            {
                ShortId = position.ShortId,
                BotName = _options.BotName,
                Symbol = position.Symbol,
                Side = position.Side,
                CreatedAtUtc = position.UpdatedAtUtc ?? position.CreatedAtUtc
            };
        }

        logger.LogInformation(
            "BOT8015 effective active positions loaded. Symbol={Symbol}, Count={Count}",
            symbol,
            result.Count);

        return result.Values
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();
    }

    private void EnsureSupportedSymbol(string symbol)
    {
        if (!symbol.Equals(_options.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"BOT8015 supports only '{_options.Symbol}', but received '{symbol}'.");
        }
    }

    private static bool TryExtractShortId(
        string? clientId,
        string prefix,
        out string shortId)
    {
        shortId = string.Empty;

        if (string.IsNullOrWhiteSpace(clientId)
            || !clientId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = clientId[prefix.Length..];
        shortId = remainder.Split(
            '_',
            StringSplitOptions.RemoveEmptyEntries)[0];

        return !string.IsNullOrWhiteSpace(shortId);
    }

    private static bool TryParseSide(
        string? value,
        out PositionSide side)
    {
        side = default;

        if (value?.Equals("LONG", StringComparison.OrdinalIgnoreCase) == true)
        {
            side = PositionSide.Long;
            return true;
        }

        if (value?.Equals("SHORT", StringComparison.OrdinalIgnoreCase) == true)
        {
            side = PositionSide.Short;
            return true;
        }

        return false;
    }
}
