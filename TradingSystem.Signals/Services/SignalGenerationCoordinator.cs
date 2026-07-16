using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Configuration;
using TradingSystem.Signals.History;
using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Services;

public sealed class SignalGenerationCoordinator(
    IEnumerable<ITradingSignalGenerator> generators,
    ISignalPublisher publisher,
    ITradingPipelineRecorder recorder,
    IOptions<SignalGenerationOptions> options,
    ILogger<SignalGenerationCoordinator> logger)
    : ISignalGenerationCoordinator
{
    private readonly IReadOnlyCollection<ITradingSignalGenerator> _generators = generators.ToArray();
    private readonly SignalGenerationOptions _options = options.Value;
    private readonly Dictionary<string, DateTime> _lastGeneratedAt = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public async Task ProcessAsync(MarketIndicatorSnapshot snapshot, CancellationToken cancellationToken)
    {
        foreach (var generator in _generators.Where(x =>
                     x.SupportedSymbols.Contains(snapshot.Symbol, StringComparer.OrdinalIgnoreCase)))
        {
            if (!_options.Bots.TryGetValue(generator.BotName, out var bot) || !bot.Enabled)
                continue;

            if (!string.Equals(bot.Symbol, snapshot.Symbol, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(bot.Interval, snapshot.Interval, StringComparison.OrdinalIgnoreCase))
                continue;

            if (bot.Mode == SignalGenerationMode.TradingViewOnly)
                continue;

            var signal = await generator.GenerateAsync(snapshot, cancellationToken);
            if (signal is null || IsDuplicate(signal, bot.MinimumSecondsBetweenGeneratedSignals))
                continue;

            await recorder.RecordSignalReceivedAsync(new SignalReceivedRecord(
                signal.SignalId, signal.BotName, signal.StrategyVersion, signal.Symbol,
                signal.Side, signal.Source, ResolveEnvironment(), signal.SignalTimeUtc,
                signal.ReferencePrice, null, signal.CandleOpenTimeUtc, signal.Interval,
                signal.Reason, signal.Metadata), cancellationToken);

            if (bot.Mode is SignalGenerationMode.InternalLive or SignalGenerationMode.Compare)
                await publisher.PublishAsync(signal, cancellationToken);
            else
                logger.LogInformation(
                    "Shadow signal recorded. Bot={BotName}, Side={Side}, Symbol={Symbol}, Reason={Reason}",
                    signal.BotName, signal.Side, signal.Symbol, signal.Reason);
        }
    }

    private bool IsDuplicate(GeneratedTradingSignal signal, int minimumSeconds)
    {
        var key = $"{signal.BotName}:{signal.Symbol}:{signal.Side}";
        lock (_gate)
        {
            if (_lastGeneratedAt.TryGetValue(key, out var last) &&
                signal.SignalTimeUtc - last < TimeSpan.FromSeconds(Math.Max(0, minimumSeconds)))
                return true;

            _lastGeneratedAt[key] = signal.SignalTimeUtc;
            return false;
        }
    }

    private static string ResolveEnvironment()
        => Environment.GetEnvironmentVariable("TRADING_ENVIRONMENT") ?? "Demo";
}
