using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;

namespace StrategyService.Positions;

public sealed class Bot8013ActivePositionProvider(
    IOptions<Bot8013Options> options,
    IBinanceFuturesOrderClient orders,
    ILogger<Bot8013ActivePositionProvider> logger)
    : IBotActivePositionProvider
{
    private readonly Bot8013Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        EnsureSupportedSymbol(symbol);

        var openOrders = await orders.GetOpenOrdersAsync(
            symbol,
            cancellationToken);

        var positions = new List<ActivePositionView>();

        foreach (var order in openOrders)
        {
            if (!TryParseTpClientId(
                    order.ClientOrderId,
                    _options.BotName,
                    out var shortId))
            {
                continue;
            }

            if (!order.Type.Equals(
                    "LIMIT",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (order.Price <= 0)
                continue;

            if (!TryParsePositionSide(
                    order.PositionSide,
                    out var side))
            {
                logger.LogWarning(
                    "Ignoring BOT8013 order with invalid position side. ClientOrderId={ClientOrderId}, PositionSide={PositionSide}",
                    order.ClientOrderId,
                    order.PositionSide);

                continue;
            }

            positions.Add(new ActivePositionView
            {
                ShortId = shortId,
                BotName = _options.BotName,
                Symbol = symbol,
                Side = side,
                TpPrice = order.Price,
                CreatedAtUtc = order.UpdateTimeUtc
            });
        }

        logger.LogInformation(
            "BOT8013 active TP positions loaded. Symbol={Symbol}, Count={Count}",
            symbol,
            positions.Count);

        return positions
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();
    }

    private void EnsureSupportedSymbol(string symbol)
    {
        if (!symbol.Equals(
                _options.Symbol,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"BOT8013 supports only '{_options.Symbol}', but received '{symbol}'.");
        }
    }

    private static bool TryParseTpClientId(
        string? clientOrderId,
        string botName,
        out string shortId)
    {
        shortId = string.Empty;

        if (string.IsNullOrWhiteSpace(clientOrderId))
            return false;

        var prefix = $"{botName}_TP_";

        if (!clientOrderId.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        shortId = clientOrderId[prefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(shortId);
    }

    private static bool TryParsePositionSide(
        string? value,
        out PositionSide side)
    {
        side = default;

        if (value?.Equals(
                "LONG",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            side = PositionSide.Long;
            return true;
        }

        if (value?.Equals(
                "SHORT",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            side = PositionSide.Short;
            return true;
        }

        return false;
    }
}
