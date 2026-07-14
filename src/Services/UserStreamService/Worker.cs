using Microsoft.Extensions.Options;
using UserStreamService.Clients;
using UserStreamService.Configuration;
using UserStreamService.Services;

namespace UserStreamService;

public sealed class Worker(
    BinanceListenKeyClient listenKeyClient,
    BinanceUserStreamClient userStreamClient,
    HealingService healingService,
    IOptions<UserStreamOptions> options,
    TimeProvider timeProvider,
    ILogger<Worker> logger)
    : BackgroundService
{
    private readonly UserStreamOptions _options = options.Value;
    private readonly object _stateLock = new();
    private string? _listenKey;
    private DateTimeOffset? _disconnectedAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
            RunStreamLoopAsync(stoppingToken),
            RunKeepAliveLoopAsync(stoppingToken));
    }

    private async Task RunStreamLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var listenKey = await listenKeyClient.CreateAsync(stoppingToken);
                SetListenKey(listenKey);

                await userStreamClient.RunAsync(
                    listenKey,
                    OnConnectedAsync,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Binance user stream loop failed.");
            }
            finally
            {
                ClearListenKeyAndMarkDisconnected();
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    "Reconnecting user stream. DelaySeconds={DelaySeconds}",
                    _options.ReconnectDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), stoppingToken);
            }
        }
    }

    private async Task RunKeepAliveLoopAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.ListenKeyKeepAliveSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var listenKey = GetListenKey();
            if (string.IsNullOrWhiteSpace(listenKey))
                continue;

            try
            {
                await listenKeyClient.KeepAliveAsync(listenKey, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Binance listen key keepalive failed.");
            }
        }
    }

    private async Task OnConnectedAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset? disconnectedAt;

        lock (_stateLock)
        {
            disconnectedAt = _disconnectedAt;
            _disconnectedAt = null;
        }

        if (disconnectedAt is null)
        {
            logger.LogInformation("Binance user stream connected for the first time.");
            return;
        }

        var downtime = timeProvider.GetUtcNow() - disconnectedAt.Value;
        logger.LogInformation("Binance user stream reconnected. DowntimeSeconds={DowntimeSeconds:F1}", downtime.TotalSeconds);
        await healingService.PublishAfterReconnectAsync(downtime, cancellationToken);
    }

    private void SetListenKey(string listenKey)
    {
        lock (_stateLock)
            _listenKey = listenKey;
    }

    private string? GetListenKey()
    {
        lock (_stateLock)
            return _listenKey;
    }

    private void ClearListenKeyAndMarkDisconnected()
    {
        lock (_stateLock)
        {
            _listenKey = null;
            _disconnectedAt ??= timeProvider.GetUtcNow();
        }
    }
}
