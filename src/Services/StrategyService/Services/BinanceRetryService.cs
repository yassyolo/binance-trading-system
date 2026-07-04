namespace StrategyService.Services;

public sealed class BinanceRetryService
{
    private readonly ILogger<BinanceRetryService> _logger;

    public BinanceRetryService(ILogger<BinanceRetryService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        string operation,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        int attempts = 3)
    {
        Exception? lastException = null;

        for (var i = 1; i <= attempts; i++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (i < attempts)
            {
                lastException = ex;

                _logger.LogWarning(
                    ex,
                    "Binance operation failed. Operation={Operation}, Attempt={Attempt}/{Attempts}",
                    operation,
                    i,
                    attempts);

                await Task.Delay(TimeSpan.FromMilliseconds(250 * i), cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException($"Binance operation failed: {operation}");
    }

    public async Task ExecuteAsync(
        string operation,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        int attempts = 3)
    {
        await ExecuteAsync(
            operation,
            async ct =>
            {
                await action(ct);
                return true;
            },
            cancellationToken,
            attempts);
    }
}