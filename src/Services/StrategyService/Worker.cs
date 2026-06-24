using StrategyService.Models;
using StrategyService.Services;

namespace StrategyService;

public sealed class Worker : BackgroundService
{
    private readonly SignalProcessor _signalProcessor;
    private readonly ILogger<Worker> _logger;

    public Worker(
        SignalProcessor signalProcessor,
        ILogger<Worker> logger)
    {
        _signalProcessor = signalProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StrategyService started.");

        await _signalProcessor.ProcessAsync(
            new Signal
            {
                Action = "long",
                Symbol = "BTCUSDC",
                Source = "startup-test"
            },
            stoppingToken);

        await Task.Delay(3000, stoppingToken);

        await _signalProcessor.ProcessAsync(
            new Signal
            {
                Action = "short",
                Symbol = "BTCUSDC",
                Source = "startup-test"
            },
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}