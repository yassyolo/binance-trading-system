using StrategyService.Models;
using StrategyService.Strategies;

namespace StrategyService.Services;

public sealed class SignalProcessor
{
    private readonly Bot8011Strategy _bot8011;
    private readonly ILogger<SignalProcessor> _logger;

    public SignalProcessor(
        Bot8011Strategy bot8011,
        ILogger<SignalProcessor> logger)
    {
        _bot8011 = bot8011;
        _logger = logger;
    }

    public async Task ProcessAsync(
        Signal signal,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Signal received. Action={Action}, Symbol={Symbol}, Source={Source}",
            signal.Action,
            signal.Symbol,
            signal.Source);

        await _bot8011.ProcessSignalAsync(signal, cancellationToken);
    }
}