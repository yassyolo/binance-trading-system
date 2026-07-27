using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.Startup;

public sealed class BinanceStartupService(
    IEnumerable<IBinanceTradingConfiguration> configurations,
    IBinanceFuturesOrderClient orders,
    ILogger<BinanceStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await orders.SetHedgeModeAsync(cancellationToken);
            logger.LogInformation("Binance hedge mode requested.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not set hedge mode. It may already be enabled or the account may reject the request.");
        }

        var configurationsBySymbol = configurations
            .GroupBy(configuration => configuration.Symbol.Trim().ToUpperInvariant())
            .ToArray();

        foreach (var symbolGroup in configurationsBySymbol)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var leverages = symbolGroup.Select(configuration => configuration.Leverage).Distinct().ToArray();
            if (leverages.Length > 1)
            {
                logger.LogWarning(
                    "Bots configured different leverage values for {Symbol}: {Leverages}. Binance leverage is account-wide per symbol; using the highest configured value.",
                    symbolGroup.Key,
                    string.Join(", ", leverages.OrderBy(value => value)));
            }

            var leverage = leverages.Max();
            try
            {
                await orders.SetLeverageAsync(symbolGroup.Key, leverage, cancellationToken);
                logger.LogInformation("Binance leverage set. Symbol = {Symbol}, Leverage = {Leverage}", symbolGroup.Key, leverage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not set leverage. Symbol = {Symbol}, Leverage = {Leverage}", symbolGroup.Key, leverage);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
