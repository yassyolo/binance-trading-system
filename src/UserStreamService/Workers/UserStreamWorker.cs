using Microsoft.Extensions.Options;
using TradingSystem.Binance.UserStream.Configuration;
using TradingSystem.Binance.UserStream.Contracts;
using UserStreamService.Configuration;
using UserStreamService.Services;

namespace UserStreamService.Workers;

public sealed class UserStreamWorker(
    IBinanceListenKeyClient listenKeys,
    IBinanceUserStreamClient stream,
    UserStreamEventProcessor processor,
    HealingPublisher healing,
    IOptions<BinanceUserStreamOptions> binanceOptions,
    IOptions<UserStreamServiceOptions> serviceOptions,
    TimeProvider time,
    ILogger<UserStreamWorker> logger)
    : BackgroundService
{
    private readonly object _sync = new();
    private readonly BinanceUserStreamOptions _binanceOptions = binanceOptions.Value;
    private readonly UserStreamServiceOptions _serviceOptions = serviceOptions.Value;

    private string? _listenKey;
    private DateTimeOffset? _disconnectedAtUtc;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A process restart can miss Redis Pub/Sub events that happened while the
        // process was down. Treat the first successful connection as a recovery
        // boundary and publish the same REST healing snapshot used after reconnect.
        lock (_sync)
        {
            _disconnectedAtUtc =
                time.GetUtcNow() -
                TimeSpan.FromSeconds(Math.Max(0, _serviceOptions.MinDowntimeForHealingSeconds));
        }

        return Task.WhenAll(
            StreamLoopAsync(stoppingToken),
            KeepAliveLoopAsync(stoppingToken));
    }

    private async Task StreamLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var listenKey = await listenKeys.CreateAsync(ct);

                lock (_sync)
                    _listenKey = listenKey;

                await stream.RunAsync(
                    listenKey,
                    processor.ProcessAsync,
                    OnConnectedAsync,
                    ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
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

            if (!ct.IsCancellationRequested)
                await Task.Delay(
                    TimeSpan.FromSeconds(_serviceOptions.ReconnectDelaySeconds),
                    ct);
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_binanceOptions.ListenKeyKeepAliveSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                string? listenKey;

                lock (_sync)
                    listenKey = _listenKey;

                if (string.IsNullOrWhiteSpace(listenKey))
                    continue;

                try
                {
                    await listenKeys.KeepAliveAsync(listenKey, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Listen-key keepalive failed.");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private async Task OnConnectedAsync(CancellationToken ct)
    {
        DateTimeOffset? disconnectedAtUtc;

        lock (_sync)
        {
            disconnectedAtUtc = _disconnectedAtUtc;
            _disconnectedAtUtc = null;
        }

        logger.LogInformation("Binance user stream connected.");

        if (disconnectedAtUtc is null)
            return;

        var downtime = time.GetUtcNow() - disconnectedAtUtc.Value;

        logger.LogInformation(
            "Publishing post-connect healing snapshot. DowntimeSeconds = {DowntimeSeconds}",
            Math.Round(downtime.TotalSeconds, 1));

        await healing.PublishAfterReconnectAsync(downtime, ct);
    }
}
