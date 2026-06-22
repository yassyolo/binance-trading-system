using UserStreamService.Clients;

namespace UserStreamService;

public sealed class Worker : BackgroundService
{
    private readonly BinanceListenKeyClient _listenKeyClient;
    private readonly BinanceUserStreamClient _userStreamClient;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    private string? _listenKey;

    public Worker(
        BinanceListenKeyClient listenKeyClient,
        BinanceUserStreamClient userStreamClient,
        ILogger<Worker> logger,
        IConfiguration configuration)
    {
        _listenKeyClient = listenKeyClient;
        _userStreamClient = userStreamClient;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var keepAliveTask = RunKeepAliveLoopAsync(stoppingToken);
        var streamTask = RunUserStreamLoopAsync(stoppingToken);

        await Task.WhenAll(keepAliveTask, streamTask);
    }

    private async Task RunUserStreamLoopAsync(CancellationToken stoppingToken)
    {
        var reconnectDelaySeconds =
            _configuration.GetValue<int>("UserStream:ReconnectDelaySeconds", 5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _listenKey = await _listenKeyClient.CreateListenKeyAsync(stoppingToken);

                await _userStreamClient.RunAsync(_listenKey, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User stream loop failed.");
            }

            _listenKey = null;

            _logger.LogWarning(
                "Reconnecting user stream in {DelaySeconds}s...",
                reconnectDelaySeconds);

            await Task.Delay(
                TimeSpan.FromSeconds(reconnectDelaySeconds),
                stoppingToken);
        }
    }

    private async Task RunKeepAliveLoopAsync(CancellationToken stoppingToken)
    {
        var keepAliveSeconds =
            _configuration.GetValue<int>("UserStream:ListenKeyKeepAliveSeconds", 1800);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(keepAliveSeconds),
                stoppingToken);

            if (string.IsNullOrWhiteSpace(_listenKey))
                continue;

            try
            {
                await _listenKeyClient.KeepAliveAsync(_listenKey, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ListenKey keepalive failed.");
            }
        }
    }
}