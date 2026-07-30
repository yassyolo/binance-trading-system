using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingSystem.PaperTrading;

namespace StrategyService.Runtime;

public sealed class PaperPositionCloseWorker(
    PaperTradeExecutor executor,
    ILogger<PaperPositionCloseWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var result = await executor.CloseAsync(
            "BOT8012",
            "P2607301333231084",
            "MANUAL_PAPER_TEST_CLOSE",
            stoppingToken);

        logger.LogInformation(
            "Manual paper close completed. Success = {Success}, OrderId = {OrderId}, Message = {Message}",
            result.Succeeded,
            result.ShortId,
            result.Reason);
    }
}