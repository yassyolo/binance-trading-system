using Microsoft.Extensions.Options;
using TradingSystem.Binance.UserStream.Configuration;
using TradingSystem.Binance.UserStream.Contracts;
using UserStreamService.Configuration;
using UserStreamService.Services;

namespace UserStreamService.Workers;

public sealed class UserStreamWorker(
    IBinanceListenKeyClient listenKeys,
    IBinanceUserStreamClient streamClient,
    UserStreamEventProcessor eventProcessor,
    HealingPublisher healingPublisher,
    IOptions<BinanceUserStreamOptions> binanceOptions,
    IOptions<UserStreamServiceOptions> userStreamOptions,
    TimeProvider time,
    ILogger<UserStreamWorker> logger)
    : BackgroundService
{
    private readonly object _sync = new();
    private readonly BinanceUserStreamOptions _binanceOptions = binanceOptions.Value;
    private readonly UserStreamServiceOptions _serviceOptions = userStreamOptions.Value;

    private string? _listenKey;
    private DateTimeOffset? _disconnectedAtUtc;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        lock (_sync)
        {
            _disconnectedAtUtc = time.GetUtcNow() - TimeSpan.FromSeconds(Math.Max(0, _serviceOptions.MinDowntimeForHealingSeconds));
        }

        return Task.WhenAll(StreamLoopAsync(stoppingToken), KeepAliveLoopAsync(stoppingToken));
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

                await streamClient.RunAsync(listenKey, eventProcessor.ProcessAsync, OnConnectedAsync, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Binance user streamClient failed.");
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
                await Task.Delay(TimeSpan.FromSeconds(_serviceOptions.ReconnectDelaySeconds), ct);
        }
    }

    private async Task KeepAliveLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_binanceOptions.ListenKeyKeepAliveSeconds));

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
        {}
    }

    private async Task OnConnectedAsync(CancellationToken ct)
    {
        DateTimeOffset? disconnectedAtUtc;

        lock (_sync)
        {
            disconnectedAtUtc = _disconnectedAtUtc;
            _disconnectedAtUtc = null;
        }

        logger.LogInformation("Binance user streamClient connected.");

        if (disconnectedAtUtc is null)
            return;

        var downtime = time.GetUtcNow() - disconnectedAtUtc.Value;

        logger.LogInformation("Publishing post-connect healingPublisher snapshot. DowntimeSeconds = {DowntimeSeconds}", Math.Round(downtime.TotalSeconds, 1));

        await healingPublisher.PublishAfterReconnectAsync(downtime, ct);
    }
}
