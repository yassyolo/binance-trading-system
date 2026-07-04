using StrategyService.Services;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace StrategyService;

public sealed class Worker : BackgroundService
{
    private readonly SignalProcessor _signalProcessor;
    private readonly PositionManager _positionManager;
    private readonly ILogger<Worker> _logger;

    public Worker(
        SignalProcessor signalProcessor,
        PositionManager positionManager,
        ILogger<Worker> logger)
    {
        _signalProcessor = signalProcessor;
        _positionManager = positionManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StrategyService started.");

        var testPosition = new BotPosition
        {
            ShortId = "test-001",
            BotName = "BOT8011",
            Symbol = "BTCUSDC",
            Side = PositionSide.Long,
            Mode = PositionMode.Stop3,
            Quantity = 0.001m,
            RemainingQuantity = 0.001m,
            Status = PositionStatus.Test
        };

        await _positionManager.AddAsync(testPosition, stoppingToken);

        var positions = await _positionManager.GetActivePositionsAsync(
            "BOT8011",
            stoppingToken);

        _logger.LogInformation(
            "Redis smoke test positions count: {Count}",
            positions.Count);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}