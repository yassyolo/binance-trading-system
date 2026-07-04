namespace StrategyService.Services;

public sealed class Bot8011ManualRecoveryHostedService : IHostedService
{
    private readonly Bot8011ManualPositionRecoveryService _recovery;
    private readonly ILogger<Bot8011ManualRecoveryHostedService> _logger;

    public Bot8011ManualRecoveryHostedService(
        Bot8011ManualPositionRecoveryService recovery,
        ILogger<Bot8011ManualRecoveryHostedService> logger)
    {
        _recovery = recovery;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var recovered = await _recovery.RecoverAsync(cancellationToken);

        _logger.LogInformation(
            "BOT8011 manual recovery completed. Recovered={Recovered}",
            recovered);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}