using Microsoft.Extensions.Options;
using StrategyService.Configuration;
using TradingSystem.Binance.Orders;

namespace StrategyService.Services;

public sealed class BinanceStartupService : IHostedService
{
    private readonly Bot8011Options _options;
    private readonly IBinanceFuturesOrderClient _orders;
    private readonly ILogger<BinanceStartupService> _logger;

    public BinanceStartupService(
        IOptions<Bot8011Options> options,
        IBinanceFuturesOrderClient orders,
        ILogger<BinanceStartupService> logger)
    {
        _options = options.Value;
        _orders = orders;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _orders.SetHedgeModeAsync(cancellationToken);

            _logger.LogInformation("Binance hedge mode requested.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not set hedge mode. It may already be enabled.");
        }

        try
        {
            await _orders.SetLeverageAsync(
                _options.Symbol,
                _options.Leverage,
                cancellationToken);

            _logger.LogInformation(
                "Binance leverage set. Symbol={Symbol}, Leverage={Leverage}",
                _options.Symbol,
                _options.Leverage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not set leverage.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}