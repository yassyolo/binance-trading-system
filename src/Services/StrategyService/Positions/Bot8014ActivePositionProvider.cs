using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Binance.Orders.Contracts;
using TradingSystem.Domain.Enums;

namespace StrategyService.Positions;

public sealed class Bot8014ActivePositionProvider(
    IOptions<Bot8014Options> options,
    IBinanceFuturesOrderClient orders,
    ILogger<Bot8014ActivePositionProvider> logger)
    : IBotActivePositionProvider
{
    private readonly Bot8014Options _options = options.Value;

    public string BotName => _options.BotName;

    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        var openOrders = await orders.GetOpenOrdersAsync(symbol, cancellationToken);

        var positions = openOrders
            .Where(x => x.ClientOrderId.StartsWith($"{_options.BotName}_TP_", StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Type.Equals("LIMIT", StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Price > 0)
            .Select(x => new ActivePositionView
            {
                ShortId = ExtractShortId(x.ClientOrderId),
                BotName = _options.BotName,
                Symbol = symbol,
                Side = ParseSide(x.PositionSide),
                TpPrice = x.Price,
                CreatedAtUtc = x.UpdateTimeUtc
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.ShortId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        logger.LogInformation(
            "BOT8014 active TP positions loaded. Symbol={Symbol}, Count={Count}",
            symbol,
            positions.Count);

        return positions;
    }

    private static string ExtractShortId(string clientOrderId)
    {
        var parts = clientOrderId.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? parts[2] : string.Empty;
    }

    private static PositionSide ParseSide(string positionSide)
        => positionSide.Equals("SHORT", StringComparison.OrdinalIgnoreCase)
            ? PositionSide.Short
            : PositionSide.Long;
}