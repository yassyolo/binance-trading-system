using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;

namespace StrategyService.Positions;

public sealed class Bot8012ActivePositionProvider(
    IOptions<Bot8012Options> options,
    IBinanceFuturesOrderClient orders,
    ILogger<Bot8012ActivePositionProvider> logger)
    : IBotActivePositionProvider
{
    private readonly Bot8012Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        EnsureSupportedSymbol(symbol);

        var openOrders = await orders.GetOpenOrdersAsync(
            symbol,
            cancellationToken);

        var result = new List<ActivePositionView>();

        foreach (var order in openOrders)
        {
            if (!TryParseBotTpClientId(
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
                    "Ignoring BOT8012 TP order with unsupported position side. ClientOrderId={ClientOrderId}, PositionSide={PositionSide}",
                    order.ClientOrderId,
                    order.PositionSide);

                continue;
            }

            result.Add(new ActivePositionView
            {
                ShortId = shortId,
                BotName = _options.BotName,
                Symbol = symbol,
                Side = side,
                TpPrice = order.Price,
                CreatedAtUtc = order.UpdateTimeUtc
            });
        }

        return result
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
                $"BOT8012 supports only '{_options.Symbol}', but received '{symbol}'.");
        }
    }

    private static bool TryParseBotTpClientId(
        string? clientOrderId,
        string botName,
        out string shortId)
    {
        shortId = string.Empty;

        if (string.IsNullOrWhiteSpace(clientOrderId))
            return false;

        var expectedPrefix = $"{botName}_TP_";

        if (!clientOrderId.StartsWith(
                expectedPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        shortId = clientOrderId[expectedPrefix.Length..].Trim();

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
