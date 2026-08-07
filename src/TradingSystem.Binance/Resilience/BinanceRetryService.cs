using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using TradingSystem.Binance.Exceptions;
using TradingSystem.Binance.Resilience.Configuration;

namespace TradingSystem.Binance.Resilience;

public sealed class BinanceRetryService(
    IOptions<BinanceRetryOptions> options,
    ILogger<BinanceRetryService> logger)
{
    private readonly BinanceRetryOptions _options = options.Value;

    public async Task<T> ExecuteAsync<T>(string operationName, Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(operation);

        var maximumAttempts = _options.Enabled ? _options.MaximumAttempts : 1;

        for (var attempt = 1; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await operation(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < maximumAttempts && IsTransient(ex, ct))
            {
                var delay = CalculateDelay(attempt);

                logger.LogWarning(ex, "Transient Binance failure during {OperationName}. Attempt {Attempt}/{MaximumAttempts}; retrying in {DelayMilliseconds} ms.", operationName, attempt, maximumAttempts, delay.TotalMilliseconds);

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }

    public async Task ExecuteAsync(string operationName, Func<CancellationToken, Task> operation, CancellationToken ct)
    {
        await ExecuteAsync<object?>(
            operationName,
            async ct =>
            {
                await operation(ct).ConfigureAwait(false);
                return null;
            },
            ct).ConfigureAwait(false);
    }

    internal static bool IsTransient(Exception exception, CancellationToken callerToken)
    {
        if (exception is OperationCanceledException)
            return !callerToken.IsCancellationRequested;

        if (exception is TimeoutException or HttpRequestException)
            return true;

        return exception is BinanceApiException apiException &&
               (apiException.StatusCode == (HttpStatusCode)408 ||
                apiException.StatusCode == (HttpStatusCode)429 ||
                (int)apiException.StatusCode >= 500);
    }

    private TimeSpan CalculateDelay(int failedAttempt)
    {
        var exponentialDelay = _options.InitialDelayMilliseconds *
                               Math.Pow(_options.BackoffMultiplier, failedAttempt - 1);
        var boundedDelay = Math.Min(exponentialDelay, _options.MaximumDelayMilliseconds);

        if (_options.UseJitter && boundedDelay > 0)
            boundedDelay *= 0.8 + (Random.Shared.NextDouble() * 0.4);

        return TimeSpan.FromMilliseconds(Math.Max(0, boundedDelay));
    }
}
