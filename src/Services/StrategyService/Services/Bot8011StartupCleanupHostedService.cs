namespace StrategyService.Services;

public sealed class Bot8011StartupCleanupHostedService : IHostedService
{
    private readonly Bot8011RedisCleanupService _cleanup;
    private readonly ILogger<Bot8011StartupCleanupHostedService> _logger;

    public Bot8011StartupCleanupHostedService(
        Bot8011RedisCleanupService cleanup,
        ILogger<Bot8011StartupCleanupHostedService> logger)
    {
        _cleanup = cleanup;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var cleaned = await _cleanup.CleanupGhostPositionsAsync(cancellationToken);

        _logger.LogInformation(
            "BOT8011 Redis cleanup completed. Cleaned={Cleaned}",
            cleaned);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}