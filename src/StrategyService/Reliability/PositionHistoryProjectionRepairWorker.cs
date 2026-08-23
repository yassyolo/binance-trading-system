using StrategyService.Services;
using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace StrategyService.Reliability;

public sealed class PositionHistoryProjectionRepairWorker(
    IEnumerable<IBotTradeExecutor> executors,
    IPositionStore positions,
    IBotRuntimeConfigurationProvider configurations,
    LivePositionLifecycleRecorder lifecycle,
    ILogger<PositionHistoryProjectionRepairWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            var botNames = executors
                .Select(executor => executor.BotName)
                .Where(bot => !string.IsNullOrWhiteSpace(bot))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var repaired = 0;

            foreach (var botName in botNames)
            {
                stoppingToken.ThrowIfCancellationRequested();

                var configuration = await configurations.GetAsync(botName, stoppingToken);
                if (configuration is null || !IsLive(configuration.Environment))
                    continue;

                var botPositions = await positions.GetAllAsync(botName, stoppingToken);

                foreach (var position in botPositions)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await lifecycle.RepairAsync(position, stoppingToken);
                    repaired++;
                }
            }

            logger.LogInformation(
                "Live position history projection repair completed. Positions evaluated = {Count}",
                repaired);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Live position history projection startup repair failed. Normal trading can continue.");
        }
    }

    private static bool IsLive(string environment) =>
        environment.Equals("Demo", StringComparison.OrdinalIgnoreCase) ||
        environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
