using AlligatorIndicatorService.Configuration;
using Microsoft.Extensions.Options;

namespace AlligatorIndicatorService.Services;

public sealed class AlligatorHistoryInitializer(
    IOptions<AlligatorOptions> options,
    BinanceHistoricalKlineClient historical,
    AlligatorMaEngine engine,
    ILogger<AlligatorHistoryInitializer> logger)
    : IHostedService
{
    private readonly AlligatorOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var symbol in _options.Symbols)
        {
            foreach (var interval in _options.Intervals)
            {
                var candles = await historical.GetHistoricalCandlesAsync(
                    symbol,
                    interval,
                    _options.HistoryLimit,
                    cancellationToken);

                engine.InitializeHistory(symbol, interval, candles);

                logger.LogInformation(
                    "Alligator history initialized. Symbol={Symbol}, Interval={Interval}, Count={Count}",
                    symbol,
                    interval,
                    candles.Count);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
