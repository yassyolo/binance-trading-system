namespace StrategyService.Services;

public sealed class Bot8011ManualRecoveryHostedService(
        Bot8011ManualPositionRecoveryService recovery,
        ILogger<Bot8011ManualRecoveryHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var recovered = await recovery.RecoverAsync(cancellationToken);

        logger.LogInformation(
            "BOT8011 manual recovery completed. Recovered={Recovered}",
            recovered);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}