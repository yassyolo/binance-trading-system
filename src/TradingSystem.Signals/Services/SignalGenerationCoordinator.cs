using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Observability.Environment;
using TradingSystem.Observability.History;
using TradingSystem.Signals.Abstractions;
using TradingSystem.Signals.Configuration;
using TradingSystem.Signals.Models;

namespace TradingSystem.Signals.Services;

public sealed class SignalGenerationCoordinator(
    IEnumerable<ITradingSignalGenerator> generators, 
    ISignalPublisher publisher, 
    IDistributedSignalThrottleStore throttle, 
    ITradingPipelineRecorder history, 
    ITradingEnvironmentProvider environment, 
    IOptions<SignalGenerationOptions> options, 
    ILogger<SignalGenerationCoordinator> logger):
    ISignalGenerationCoordinator
{
    readonly ITradingSignalGenerator[] _generators = generators.ToArray();
    readonly SignalGenerationOptions _options = options.Value;
 public async Task ProcessAsync(MarketIndicatorSnapshot snapshot, CancellationToken ct)
    {
        foreach(var g in _generators.Where(x => x.SupportedSymbols.Contains(snapshot.Symbol, StringComparer.OrdinalIgnoreCase)))
        {
            if(!_options.Bots.TryGetValue(g.BotName, out var b) || !b.Enabled || b.Mode==SignalGenerationMode.TradingViewOnly)
                continue;
            if(!b.Symbol.Equals(snapshot.Symbol, StringComparison.OrdinalIgnoreCase) || !b.Interval.Equals(snapshot.Interval, StringComparison.OrdinalIgnoreCase))
                continue;
            var s = await g.GenerateAsync(snapshot, ct);
            if(s is null)
                continue;
            if(!await throttle.TryAcquireAsync(s.BotName, s.Symbol, s.Side, s.SignalTimeUtc, TimeSpan.FromSeconds(Math.Max(0, b.MinimumSecondsBetweenGeneratedSignals)), ct))
                continue;
            await history.RecordSignalAsync(new(s.SignalId, s.BotName, s.StrategyVersion, s.Symbol, s.Action, s.Source, environment.EnvironmentName, s.GeneratedAtUtc, s.Price, s.CandleOpenTimeUtc, s.Interval, s.Reason, null, s.Metadata), ct);
            if(b.Mode is SignalGenerationMode.InternalLive or SignalGenerationMode.Compare)
                await publisher.PublishAsync(s, ct);
            else logger.LogInformation("Shadow signal recorded. Bot = {Bot} Side = {Side} Symbol = {Symbol}", s.BotName, s.Side, s.Symbol);}}
}
