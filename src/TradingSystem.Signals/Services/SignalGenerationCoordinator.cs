using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.Pipeline;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Configuration;
using TradingSystem.Signals.Contracts;
using TradingSystem.Signals.Models;
using TradingSystem.Signals.Models.Enums;

namespace TradingSystem.Signals.Services;

public sealed class SignalGenerationCoordinator(
    IEnumerable<ITradingSignalGenerator> generators,
    ISignalPublisher publisher,
    IDistributedSignalThrottleStore signalThrottle,
    ITradingPipelineRecorder history,
    ITradingEnvironmentProvider environment,
    IOptions<SignalGenerationOptions> options,
    ILogger<SignalGenerationCoordinator> logger)
    : ISignalGenerationCoordinator
{
    private static readonly TimeSpan ProcessedSignalRetention = TimeSpan.FromDays(7);

    private readonly IReadOnlyDictionary<string, ITradingSignalGenerator[]> _generatorsBySymbol =
        generators.SelectMany(g => g.SupportedSymbols
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(s => new { Symbol = s, Generator = g }))
            .GroupBy(i => i.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(i => i.Generator).Distinct().ToArray(),
            StringComparer.OrdinalIgnoreCase);

    private readonly SignalGenerationOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, DateTime> _processedSignalIds = new(StringComparer.OrdinalIgnoreCase);

    public async Task ProcessAsync(MarketIndicatorSnapshot snapshot, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!_generatorsBySymbol.TryGetValue(snapshot.Symbol, out var generators))
            return;

        CleanupOldProcessedSignals();

        foreach (var generator in generators)
        {
            ct.ThrowIfCancellationRequested();

            if (!_options.Bots.TryGetValue(generator.BotName, out var botOptions) 
                || !botOptions.Enabled
                || botOptions.Mode == SignalGenerationMode.TradingViewOnly)
                continue;

            if (!botOptions.Symbol.Equals(snapshot.Symbol, StringComparison.OrdinalIgnoreCase) 
                || !botOptions.Interval.Equals(snapshot.Interval, StringComparison.OrdinalIgnoreCase))
                continue;

            var signal = await generator.GenerateAsync(snapshot, ct);
            if (signal is null)
                continue;

            ValidateGeneratedSignal(generator, snapshot, signal);

            signal = signal with { SignalId = BuildDeterministicSignalId(generator, snapshot, signal) };

            if (!_processedSignalIds.TryAdd(signal.SignalId, snapshot.CandleCloseTimeUtc))
                continue;

            try
            {
                var minimumInterval = TimeSpan.FromSeconds(botOptions.MinimumSecondsBetweenGeneratedSignals);
                var acquired = await signalThrottle.TryAcquireAsync(signal.BotName, signal.Symbol, signal.Action, signal.GeneratedAtUtc, minimumInterval, ct);
                if (!acquired)
                    continue;

                await RecordSignalWithRetryAsync(signal, ct);

                if (botOptions.Mode is SignalGenerationMode.InternalLive)
                {
                    await publisher.PublishAsync(signal, ct);
                }
                else
                {
                    logger.LogInformation("Shadow signal recorded. Bot = {Bot} Side = {Side} Symbol = {Symbol}", signal.BotName, signal.Action, signal.Symbol);
                }
            }
            catch
            {
                _processedSignalIds.TryRemove(signal.SignalId, out _);
                throw;
            }
        }
    }

    private async Task RecordSignalWithRetryAsync(GeneratedTradingSignal signal, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        var attempt = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                await history.RecordSignalAsync(new(
                    signal.SignalId,
                    signal.BotName,
                    signal.StrategyVersion,
                    signal.Symbol,
                    signal.Action,
                    signal.Source,
                    environment.EnvironmentName,
                    signal.GeneratedAtUtc,
                    signal.Price,
                    signal.CandleOpenTimeUtc,
                    signal.Interval,
                    signal.Reason,
                    null,
                    signal.Metadata), ct);
               
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    throw;

                var delay = TimeSpan.FromSeconds(Math.Min(attempt, 5));
                if (delay > remaining)
                    delay = remaining;

                logger.LogWarning(ex, "Signal history persistence failed. SignalId = {SignalId}, Attempt = {Attempt}. Retrying in {Delay}.", signal.SignalId, attempt, delay);

                await Task.Delay(delay, ct);
            }
        }
    }

    private void CleanupOldProcessedSignals()
    {
        var cutoff = DateTime.UtcNow - ProcessedSignalRetention;

        foreach (var item in _processedSignalIds)
        {
            if (item.Value < cutoff)
                _processedSignalIds.TryRemove(item.Key, out _);
        }
    }

    private static string BuildDeterministicSignalId(ITradingSignalGenerator generator, MarketIndicatorSnapshot snapshot, GeneratedTradingSignal signal)
    {
        var identity = string.Join(
            '|',
            generator.BotName.Trim().ToUpperInvariant(),
            generator.StrategyVersion.Trim(),
            snapshot.Symbol.Trim().ToUpperInvariant(),
            snapshot.Interval.Trim().ToUpperInvariant(),
            snapshot.CandleOpenTimeUtc.ToUniversalTime().Ticks,
            snapshot.CandleCloseTimeUtc.ToUniversalTime().Ticks,
            signal.Action.Trim().ToUpperInvariant());

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }

    private static void ValidateGeneratedSignal(ITradingSignalGenerator generator, MarketIndicatorSnapshot snapshot, GeneratedTradingSignal signal)
    {
        if (!signal.BotName.Equals(generator.BotName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Generator '{generator.BotName}' produced signal for bot '{signal.BotName}'.");

        if (!signal.Symbol.Equals(snapshot.Symbol, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Generator '{generator.BotName}' produced signal for symbol '{signal.Symbol}' while processing '{snapshot.Symbol}'.");

        if (!signal.Interval.Equals(snapshot.Interval, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Generator '{generator.BotName}' produced interval '{signal.Interval}' while processing '{snapshot.Interval}'.");

        if (string.IsNullOrWhiteSpace(signal.SignalId))
            throw new InvalidOperationException($"Generator '{generator.BotName}' produced an empty signal id.");

        if (signal.Price <= 0)
            throw new InvalidOperationException($"Generator '{generator.BotName}' produced a non-positive signal price.");
    }
}
