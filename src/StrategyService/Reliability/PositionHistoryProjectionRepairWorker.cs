using TradingSystem.Application.Execution.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.BotRuntime.Configuration.Contracts;

namespace StrategyService.Reliability;

public sealed class PositionHistoryProjectionRepairWorker(
    IEnumerable<IBotTradeExecutor> executors,
    IPositionStore positionStore,
    IBotRuntimeConfigurationProvider configProvider,
    LivePositionLifecycleRecorder lifecycleRecorder,
    ILogger<PositionHistoryProjectionRepairWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);

            var botNames = executors.Select(e => e.BotName)
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var repaired = 0;

            foreach (var botName in botNames)
            {
                ct.ThrowIfCancellationRequested();

                var config = await configProvider.GetAsync(botName, ct);
                if (config is null || !IsLive(config.Environment))
                    continue;

                var botPositions = await positionStore.GetAllAsync(botName, ct);
                foreach (var position in botPositions)
                {
                    ct.ThrowIfCancellationRequested();
                   
                    await lifecycleRecorder.RepairAsync(position, ct);
                   
                    repaired++;
                }
            }

            logger.LogInformation("Live position history projection repair completed. Positions evaluated = {Count}", repaired);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {}
        catch (Exception ex)
        {
            logger.LogError(ex, "Live position history projection startup repair failed. Normal trading can continue.");
        }
    }

    private static bool IsLive(string env) 
        => env.Equals("Demo", StringComparison.OrdinalIgnoreCase) 
        || env.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
