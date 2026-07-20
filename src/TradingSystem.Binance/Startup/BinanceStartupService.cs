using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.Startup;

public sealed class BinanceStartupService(IEnumerable<IBinanceTradingConfiguration> configurations,  IBinanceFuturesOrderClient orders,  ILogger<BinanceStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try { await orders.SetHedgeModeAsync(cancellationToken); logger.LogInformation("Binance hedge mode requested."); }
        catch (Exception ex) { logger.LogWarning(ex,  "Could not set hedge mode. It may already be enabled."); }

        foreach (var config in configurations.GroupBy(x  =>  (x.Symbol.ToUpperInvariant(),  x.Leverage)).Select(x  =>  x.First()))
        {
            try { await orders.SetLeverageAsync(config.Symbol,  config.Leverage,  cancellationToken); logger.LogInformation("Binance leverage set. Symbol = {Symbol},  Leverage = {Leverage}",  config.Symbol,  config.Leverage); }
            catch (Exception ex) { logger.LogWarning(ex,  "Could not set leverage. Symbol = {Symbol},  Leverage = {Leverage}",  config.Symbol,  config.Leverage); }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)  =>  Task.CompletedTask;
}
