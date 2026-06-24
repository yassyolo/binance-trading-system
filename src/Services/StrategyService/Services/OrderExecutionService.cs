using StrategyService.Configuration;
using StrategyService.Models;

namespace StrategyService.Services;

public sealed class OrderExecutionService
{
    private readonly ILogger<OrderExecutionService> _logger;

    public OrderExecutionService(ILogger<OrderExecutionService> logger)
    {
        _logger = logger;
    }

    public Task<Position> OpenBot8011PositionAsync(
        string side,
        Bot8011Options options,
        CancellationToken cancellationToken = default)
    {
        var entryPrice = 100000m; // temporary fake price

        var tpPrice = side == "LONG"
            ? entryPrice * (1 + options.TakeProfitPercent / 100)
            : entryPrice * (1 - options.TakeProfitPercent / 100);

        var slPrice = side == "LONG"
            ? entryPrice * (1 - options.StopLossPercent / 100)
            : entryPrice * (1 + options.StopLossPercent / 100);

        var position = new Position
        {
            PositionId = Guid.NewGuid().ToString("N")[..8],
            Symbol = options.Symbol,
            Side = side,
            EntryPrice = entryPrice,
            Quantity = options.Quantity,
            RemainingQuantity = options.Quantity,
            TakeProfitPrice = tpPrice,
            StopLossPrice = slPrice,
            Stop3Price = 0,
            TakeProfitExecuted = false,
            StopLossExecuted = false,
            Stop3Active = false,
            Closed = false
        };

        _logger.LogInformation(
            "FAKE BOT8011 position opened. Side={Side}, Id={Id}, Entry={Entry}, TP={TP}, SL={SL}",
            position.Side,
            position.PositionId,
            position.EntryPrice,
            position.TakeProfitPrice,
            position.StopLossPrice);

        return Task.FromResult(position);
    }
}