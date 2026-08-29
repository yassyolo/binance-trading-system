using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.Startup;

public sealed class BinanceStartupService(
    IEnumerable<IBinanceTradingConfiguration> binanceConfigs,
    IBinanceFuturesOrderClient ordersClient,
    IOptions<BinanceFuturesOptions> options,
    ILogger<BinanceStartupService> logger)
    : IHostedService
{
    private readonly BinanceFuturesOptions _options = options.Value;

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_options.RequireSignedOperations)
        {
            logger.LogInformation("Binance signed startup operations are disabled. Hedge mode and leverage c will be skipped.");
            return;
        }

        await TrySetHedgeModeAsync(ct);

        var configurationsBySymbol = binanceConfigs.Where(c => !string.IsNullOrWhiteSpace(c.Symbol))
            .GroupBy(c => c.Symbol.Trim().ToUpperInvariant())
            .ToArray();

        foreach (var symbolGroup in configurationsBySymbol)
        {
            ct.ThrowIfCancellationRequested();

            var leverages = symbolGroup.Select(c => c.Leverage).Distinct().ToArray();
            if (leverages.Length == 0)
                continue;

            if (leverages.Length > 1)
            {
                logger.LogWarning("Bots configured different leverage values for {Symbol}: {Leverages}. " +
                    "Binance leverage is account-wide per symbol; using the highest configured x.",
                    symbolGroup.Key,
                    string.Join(", ", leverages.OrderBy(x => x)));
            }

            var leverage = leverages.Max();

            await TrySetLeverageAsync(symbolGroup.Key, leverage, ct);
        }
    }

    public Task StopAsync(CancellationToken ct)
        => Task.CompletedTask;

    private async Task TrySetHedgeModeAsync(CancellationToken ct)
    {
        try
        {
            await ordersClient.SetHedgeModeAsync(ct);

            logger.LogInformation("Binance hedge mode verified.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not verify or enable Binance hedge mode.");
        }
    }

    private async Task TrySetLeverageAsync(string symbol, int leverage, CancellationToken ct)
    {
        try
        {
            await ordersClient.SetLeverageAsync(symbol, leverage, ct);

            logger.LogInformation("Binance leverage set. Symbol = {Symbol}, Leverage = {Leverage}", symbol, leverage);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not set leverage. Symbol = {Symbol}, Leverage = {Leverage}", symbol, leverage);
        }
    }
}
