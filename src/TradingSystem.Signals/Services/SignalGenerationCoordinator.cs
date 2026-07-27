using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Configuration;
using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Services;

public sealed class SignalGenerationCoordinator : ISignalGenerationCoordinator
{
    private readonly IReadOnlyDictionary<string, ITradingSignalGenerator[]> _generatorsBySymbol;
    private readonly ISignalPublisher _publisher;
    private readonly IDistributedSignalThrottleStore _throttle;
    private readonly ITradingPipelineRecorder _history;
    private readonly ITradingEnvironmentProvider _environment;
    private readonly SignalGenerationOptions _options;
    private readonly ILogger<SignalGenerationCoordinator> _logger;

    public SignalGenerationCoordinator(
        IEnumerable<ITradingSignalGenerator> generators,
        ISignalPublisher publisher,
        IDistributedSignalThrottleStore throttle,
        ITradingPipelineRecorder history,
        ITradingEnvironmentProvider environment,
        IOptions<SignalGenerationOptions> options,
        ILogger<SignalGenerationCoordinator> logger)
    {
        _publisher = publisher;
        _throttle = throttle;
        _history = history;
        _environment = environment;
        _options = options.Value;
        _logger = logger;

        _generatorsBySymbol = generators
            .SelectMany(generator => generator.SupportedSymbols
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(symbol => new { Symbol = symbol, Generator = generator }))
            .GroupBy(item => item.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Generator).Distinct().ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task ProcessAsync(MarketIndicatorSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!_generatorsBySymbol.TryGetValue(snapshot.Symbol, out var generators))
            return;

        foreach (var generator in generators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_options.Bots.TryGetValue(generator.BotName, out var botOptions) ||
                !botOptions.Enabled ||
                botOptions.Mode == SignalGenerationMode.TradingViewOnly)
                continue;

            if (!botOptions.Symbol.Equals(snapshot.Symbol, StringComparison.OrdinalIgnoreCase) ||
                !botOptions.Interval.Equals(snapshot.Interval, StringComparison.OrdinalIgnoreCase))
                continue;

            var signal = await generator.GenerateAsync(snapshot, cancellationToken);
            if (signal is null)
                continue;

            ValidateGeneratedSignal(generator, snapshot, signal);

            var minimumInterval = TimeSpan.FromSeconds(botOptions.MinimumSecondsBetweenGeneratedSignals);
            var acquired = await _throttle.TryAcquireAsync(
                signal.BotName,
                signal.Symbol,
                signal.Action,
                signal.GeneratedAtUtc,
                minimumInterval,
                cancellationToken);

            if (!acquired)
                continue;

            await _history.RecordSignalAsync(new(
                signal.SignalId,
                signal.BotName,
                signal.StrategyVersion,
                signal.Symbol,
                signal.Action,
                signal.Source,
                _environment.EnvironmentName,
                signal.GeneratedAtUtc,
                signal.Price,
                signal.CandleOpenTimeUtc,
                signal.Interval,
                signal.Reason,
                null,
                signal.Metadata), cancellationToken);

            if (botOptions.Mode is SignalGenerationMode.InternalLive or SignalGenerationMode.Compare)
            {
                await _publisher.PublishAsync(signal, cancellationToken);
            }
            else
            {
                _logger.LogInformation(
                    "Shadow signal recorded. Bot = {Bot} Side = {Side} Symbol = {Symbol}",
                    signal.BotName,
                    signal.Action,
                    signal.Symbol);
            }
        }
    }

    private static void ValidateGeneratedSignal(
        ITradingSignalGenerator generator,
        MarketIndicatorSnapshot snapshot,
        GeneratedTradingSignal signal)
    {
        if (!signal.BotName.Equals(generator.BotName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Generator '{generator.BotName}' produced signal for bot '{signal.BotName}'.");
        if (!signal.Symbol.Equals(snapshot.Symbol, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Generator '{generator.BotName}' produced signal for symbol '{signal.Symbol}' while processing '{snapshot.Symbol}'.");
        if (!signal.Interval.Equals(snapshot.Interval, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Generator '{generator.BotName}' produced interval '{signal.Interval}' while processing '{snapshot.Interval}'.");
        if (string.IsNullOrWhiteSpace(signal.SignalId))
            throw new InvalidOperationException($"Generator '{generator.BotName}' produced an empty signal id.");
        if (signal.Price <= 0)
            throw new InvalidOperationException($"Generator '{generator.BotName}' produced a non-positive signal price.");
    }
}
