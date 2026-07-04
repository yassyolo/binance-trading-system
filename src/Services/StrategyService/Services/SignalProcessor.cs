using StrategyService.Strategies;
using TradingSystem.Domain.Signals;

namespace StrategyService.Services;

public sealed class SignalProcessor
{
    private readonly IReadOnlyDictionary<string, IBotStrategy> _strategies;
    private readonly ILogger<SignalProcessor> _logger;

    public SignalProcessor(
        IEnumerable<IBotStrategy> strategies,
        ILogger<SignalProcessor> logger)
    {
        _strategies = strategies.ToDictionary(
            x => x.BotName.ToLowerInvariant(),
            x => x);

        _logger = logger;
    }

    public async Task<bool> ProcessAsync(
        TradingSignal signal,
        CancellationToken cancellationToken = default)
    {
        var source = string.IsNullOrWhiteSpace(signal.Source)
            ? "bot8011"
            : signal.Source.Trim().ToLowerInvariant();

        _logger.LogInformation(
            "Signal received. Source={Source}, Action={Action}, Symbol={Symbol}",
            source,
            signal.Action,
            signal.Symbol);

        if (!_strategies.TryGetValue(source, out var strategy))
        {
            _logger.LogWarning(
                "Unknown signal source. Source={Source}, AvailableStrategies={Strategies}",
                source,
                string.Join(", ", _strategies.Keys));

            return false;
        }

        return await strategy.ProcessSignalAsync(signal, cancellationToken);
    }
}