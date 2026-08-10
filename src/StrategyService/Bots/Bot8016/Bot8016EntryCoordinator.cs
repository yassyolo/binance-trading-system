using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StrategyService.Bots.Bot8016.Configuration;
using StrategyService.Bots.Bot8016.Models;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Domain.Signals;

namespace StrategyService.Bots.Bot8016;

public sealed class Bot8016EntryCoordinator(
    IOptions<Bot8016Options> options,
    Bot8016EntrySignalEvaluator evaluator,
    Bot8016SignalContextStore signalContexts,
    ITradingSignalHandler handler,
    ILogger<Bot8016EntryCoordinator> logger)
{
    private readonly Bot8016Options _options = options.Value;

    public async Task ProcessAsync(Bot8016Candle candle, Bot8016IndicatorSnapshot indicator, CancellationToken cancellationToken)
    {
        var entrySignal = evaluator.Evaluate(candle, indicator);
        if (entrySignal is null)
            return;

        var signalId = BuildDeterministicSignalId(entrySignal);
        var generatedAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(candle.CloseTime).UtcDateTime;
        var signal = new TradeSignal
        {
            SignalId = signalId,
            BotName = _options.BotName,
            Symbol = candle.Symbol,
            Side = entrySignal.Side,
            Source = "alligator",
            GeneratedAtUtc = generatedAtUtc,
            SuggestedPrice = candle.Close,
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["reason"] = entrySignal.Reason,
                ["interval"] = candle.Interval,
                ["candleOpenTimeUtc"] = DateTimeOffset.FromUnixTimeMilliseconds(candle.OpenTime).UtcDateTime.ToString("O"),
                ["candleCloseTimeUtc"] = generatedAtUtc.ToString("O"),
                ["candleHigh"] = candle.High.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["candleLow"] = candle.Low.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["jaw"] = indicator.Jaw.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["teeth"] = indicator.Teeth.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["lips"] = indicator.Lips.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["sma200"] = indicator.Sma200.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }
        };

        signalContexts.Set(signalId, entrySignal);
        try
        {
            await HandleWithTransientRetryAsync(signal, cancellationToken);
        }
        finally
        {
            signalContexts.Remove(signalId);
        }
    }

    private async Task HandleWithTransientRetryAsync(TradeSignal signal, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        var attempt = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            attempt++;
            try
            {
                _ = await handler.HandleAsync(signal, ct);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    throw;
                var delay = TimeSpan.FromSeconds(Math.Min(attempt, 5));
                if (delay > remaining)
                    delay = remaining;
                logger.LogWarning(exception,
                    "BOT8016 common trading pipeline hit a transient infrastructure failure. SignalId = {SignalId}, Attempt = {Attempt}. Retrying in {Delay}.",
                    signal.SignalId, attempt, delay);
                await Task.Delay(delay, ct);
            }
        }
    }

    private string BuildDeterministicSignalId(Bot8016EntrySignal signal)
    {
        var identity = string.Join('|',
            _options.BotName.Trim().ToUpperInvariant(),
            _options.StrategyVersion.Trim(),
            signal.Candle.Symbol.Trim().ToUpperInvariant(),
            signal.Candle.Interval.Trim().ToUpperInvariant(),
            signal.Candle.OpenTime,
            signal.Candle.CloseTime,
            signal.Side.ToString().ToUpperInvariant());
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant();
    }
}
