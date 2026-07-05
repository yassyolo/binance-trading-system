using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Strategies;
using TradingSystem.Binance.Orders;
using TradingSystem.Domain.Enums;

namespace StrategyService.Positions;

public sealed class Bot8012ActivePositionProvider : IBotActivePositionProvider
{
    private readonly Bot8012Options _options;
    private readonly IBinanceFuturesOrderClient _orders;
    private readonly ILogger<Bot8012ActivePositionProvider> _logger;

    public Bot8012ActivePositionProvider(
        IOptions<Bot8012Options> options,
        IBinanceFuturesOrderClient orders,
        ILogger<Bot8012ActivePositionProvider> logger)
    {
        _options = options.Value;
        _orders = orders;
        _logger = logger;
    }

    public string BotName => _options.BotName;

    public async Task<IReadOnlyCollection<ActivePositionView>> GetActivePositionsAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        var openOrders = await _orders.GetOpenOrdersAsync(symbol, cancellationToken);

        var positions = openOrders
            .Where(x => x.ClientOrderId.StartsWith($"{_options.BotName}_TP_", StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Type.Equals("LIMIT", StringComparison.OrdinalIgnoreCase))
            .Select(x => new ActivePositionView
            {
                ShortId = ExtractShortId(x.ClientOrderId),
                BotName = _options.BotName,
                Symbol = symbol,
                Side = ParseSide(x.PositionSide),
                TpPrice = x.Price,
                CreatedAtUtc = DateTime.UtcNow
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.ShortId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        _logger.LogInformation(
            "BOT8012 active TP positions loaded. Symbol={Symbol}, Count={Count}",
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