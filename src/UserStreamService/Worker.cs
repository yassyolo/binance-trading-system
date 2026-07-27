using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.UserStream;
using UserStreamService.Configuration;
using UserStreamService.Services;

namespace UserStreamService;

public sealed class Worker(
    IBinanceListenKeyClient listenKeys,
    IBinanceUserStreamClient stream,
    UserStreamEventProcessor processor,
    HealingPublisher healing,
    IOptions<BinanceUserStreamOptions> binanceOptions,
    IOptions<UserStreamServiceOptions> serviceOptions,
    TimeProvider time,
    ILogger<Worker> logger)
    : BackgroundService
{
    private readonly object _sync = new();
    private readonly BinanceUserStreamOptions _binanceOptions = binanceOptions.Value;
    private readonly UserStreamServiceOptions _serviceOptions = serviceOptions.Value;

    private string? _listenKey;
    private DateTimeOffset? _disconnectedAtUtc;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.WhenAll(
            StreamLoopAsync(stoppingToken),
            KeepAliveLoopAsync(stoppingToken));

    private async Task StreamLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var listenKey = await listenKeys.CreateAsync(cancellationToken);

                lock (_sync)
                    _listenKey = listenKey;

                await stream.RunAsync(
                    listenKey,
                    processor.ProcessAsync,
                    OnConnectedAsync,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Binance user stream failed.");
            }
            finally
            {
                lock (_sync)
                {
                    _listenKey = null;
                    _disconnectedAtUtc ??= time.GetUtcNow();
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_serviceOptions.ReconnectDelaySeconds),
                    cancellationToken);
            }
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_binanceOptions.ListenKeyKeepAliveSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                string? listenKey;

                lock (_sync)
                    listenKey = _listenKey;

                if (string.IsNullOrWhiteSpace(listenKey))
                    continue;

                try
                {
                    await listenKeys.KeepAliveAsync(listenKey, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Listen-key keepalive failed.");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal service shutdown.
        }
    }

    private async Task OnConnectedAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset? disconnectedAtUtc;

        lock (_sync)
        {
            disconnectedAtUtc = _disconnectedAtUtc;
            _disconnectedAtUtc = null;
        }

        if (disconnectedAtUtc is null)
            return;

        var downtime = time.GetUtcNow() - disconnectedAtUtc.Value;
        await healing.PublishAfterReconnectAsync(downtime, cancellationToken);
    }
}
