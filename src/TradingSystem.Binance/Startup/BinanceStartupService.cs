using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Binance.Configuration;
using TradingSystem.Binance.Orders.Contracts;

namespace TradingSystem.Binance.Startup;

public sealed class BinanceStartupService(
    IEnumerable<IBinanceTradingConfiguration> configurations,
    IBinanceFuturesOrderClient orders,
    IOptions<BinanceFuturesOptions> binanceOptions,
    ILogger<BinanceStartupService> logger)
    : IHostedService
{
    private readonly BinanceFuturesOptions _binanceOptions =
        binanceOptions.Value;

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_binanceOptions.RequireSignedOperations)
        {
            logger.LogInformation(
                "Binance signed startup operations are disabled. " +
                "Hedge mode and leverage configuration will be skipped.");

            return;
        }

        await TrySetHedgeModeAsync(ct);

        var configurationsBySymbol = configurations
            .Where(configuration =>
                !string.IsNullOrWhiteSpace(configuration.Symbol))
            .GroupBy(configuration =>
                configuration.Symbol.Trim().ToUpperInvariant())
            .ToArray();

        foreach (var symbolGroup in configurationsBySymbol)
        {
            ct.ThrowIfCancellationRequested();

            var leverages = symbolGroup
                .Select(configuration => configuration.Leverage)
                .Distinct()
                .ToArray();

            if (leverages.Length == 0)
                continue;

            if (leverages.Length > 1)
            {
                logger.LogWarning(
                    "Bots configured different leverage values for {Symbol}: {Leverages}. " +
                    "Binance leverage is account-wide per symbol; using the highest configured value.",
                    symbolGroup.Key,
                    string.Join(
                        ", ",
                        leverages.OrderBy(value => value)));
            }

            var leverage = leverages.Max();

            await TrySetLeverageAsync(
                symbolGroup.Key,
                leverage,
                ct);
        }
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    private async Task TrySetHedgeModeAsync(CancellationToken ct)
    {
        try
        {
            await orders.SetHedgeModeAsync(ct);

            logger.LogInformation(
                "Binance hedge mode verified.");
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not verify or enable Binance hedge mode.");
        }
    }

    private async Task TrySetLeverageAsync(
        string symbol,
        int leverage,
        CancellationToken ct)
    {
        try
        {
            await orders.SetLeverageAsync(
                symbol,
                leverage,
                ct);

            logger.LogInformation(
                "Binance leverage set. Symbol = {Symbol}, Leverage = {Leverage}",
                symbol,
                leverage);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not set leverage. Symbol = {Symbol}, Leverage = {Leverage}",
                symbol,
                leverage);
        }
    }
}
