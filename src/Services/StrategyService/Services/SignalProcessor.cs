using StrategyService.Models;
using StrategyService.Strategies;

namespace StrategyService.Services;

public sealed class SignalProcessor
{
    private readonly Bot8011Strategy _bot8011;
    private readonly Bot8012Strategy _bot8012;
    private readonly Bot8013Strategy _bot8013;
    private readonly Bot8014Strategy _bot8014;
    private readonly Bot8015Strategy _bot8015;
    private readonly Bot8016Strategy _bot8016;
    private readonly ILogger<SignalProcessor> _logger;

    public SignalProcessor(
        Bot8011Strategy bot8011,
        Bot8012Strategy bot8012,
        Bot8013Strategy bot8013,
        Bot8014Strategy bot8014,
        Bot8015Strategy bot8015,
        Bot8016Strategy bot8016,
        ILogger<SignalProcessor> logger)
    {
        _bot8011 = bot8011;
        _bot8012 = bot8012;
        _bot8013 = bot8013;
        _bot8014 = bot8014;
        _bot8015 = bot8015;
        _bot8016 = bot8016;
        _logger = logger;
    }

    public async Task ProcessAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Signal received. Action={Action}, Symbol={Symbol}, Source={Source}",
            signal.Action,
            signal.Symbol,
            signal.Source);

        if (signal.Source.Equals("bot8012", StringComparison.OrdinalIgnoreCase))
        {
            await _bot8012.ProcessSignalAsync(signal, cancellationToken);
            return;
        }

        if (signal.Source.Equals("bot8013", StringComparison.OrdinalIgnoreCase))
        {
            await _bot8013.ProcessSignalAsync(signal, cancellationToken);
            return;
        }

        if (signal.Source.Equals("bot8014", StringComparison.OrdinalIgnoreCase))
        {
            await _bot8014.ProcessSignalAsync(signal, cancellationToken);
            return;
        }

        if (signal.Source.Equals("bot8015", StringComparison.OrdinalIgnoreCase))
        {
            await _bot8015.ProcessSignalAsync(signal, cancellationToken);
            return;
        }

        if (signal.Source.Equals("bot8016", StringComparison.OrdinalIgnoreCase))
        {
            await _bot8016.ProcessSignalAsync(signal, cancellationToken);
            return;
        }

        await _bot8011.ProcessSignalAsync(signal, cancellationToken);
    }
}