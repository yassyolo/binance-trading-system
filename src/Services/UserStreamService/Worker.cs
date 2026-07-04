using UserStreamService.Clients;
using UserStreamService.Services;

namespace UserStreamService;

public sealed class Worker(
    BinanceListenKeyClient listenKeyClient,
    BinanceUserStreamClient userStreamClient,
    HealingService healingService,
    ILogger<Worker> logger,
    IConfiguration configuration)
    : BackgroundService
{
    private string? _listenKey;
    private DateTime? _disconnectedAtUtc;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var keepAliveTask = RunKeepAliveLoopAsync(stoppingToken);
        var streamTask = RunUserStreamLoopAsync(stoppingToken);

        await Task.WhenAll(keepAliveTask, streamTask);
    }

    private async Task RunUserStreamLoopAsync(CancellationToken stoppingToken)
    {
        var reconnectDelaySeconds = configuration.GetValue<int>("UserStream:ReconnectDelaySeconds", 5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _listenKey = await listenKeyClient.CreateListenKeyAsync(stoppingToken);

                await userStreamClient.RunAsync(
                    _listenKey,
                    onConnected: async () =>
                    {
                        if (_disconnectedAtUtc is null)
                            return;

                        var downtimeSeconds = (DateTime.UtcNow - _disconnectedAtUtc.Value).TotalSeconds;

                        _disconnectedAtUtc = null;

                        await healingService.PublishHealingSnapshotAsync(downtimeSeconds, stoppingToken);
                    },
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "User stream loop failed.");
            }

            _listenKey = null;
            _disconnectedAtUtc = DateTime.UtcNow;

            logger.LogWarning("Reconnecting user stream in {DelaySeconds}s...", reconnectDelaySeconds);

            await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), stoppingToken);
        }
    }

    private async Task RunKeepAliveLoopAsync(CancellationToken stoppingToken)
    {
        var keepAliveSeconds = configuration.GetValue<int>("UserStream:ListenKeyKeepAliveSeconds", 1800);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(keepAliveSeconds), stoppingToken);

            if (string.IsNullOrWhiteSpace(_listenKey))
                continue;

            try
            {
                await listenKeyClient.KeepAliveAsync(_listenKey, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ListenKey keepalive failed.");
            }
        }
    }
}