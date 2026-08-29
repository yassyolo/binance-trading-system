using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine.Contracts;
using TradingSystem.Application.Positions.Contracts;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;
using TradingSystem.PortfolioManagement.Configuration;
using TradingSystem.PortfolioManagement.Models;
using TradingSystem.PortfolioManagement.Performance;
using TradingSystem.PortfolioManagement.Position;

namespace TradingSystem.PortfolioManagement.Provider;

public sealed class PortfolioSnapshotProvider(
    IOptions<PortfolioOptions> options,
    IPositionStore positionStore,
    IPaperPortfolioPositionSource paperPositionSource,
    IMarketPriceProvider marketPriceProvider,
    IPortfolioPerformanceSource performanceSource,
    IClock clock,
    ILogger<PortfolioSnapshotProvider> logger) 
    : IPortfolioSnapshotProvider
{
    private readonly PortfolioOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private PortfolioSnapshot? _cached;
    private DateTime _cacheExpiresAtUtc;

    public async Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        
        var cached = Volatile.Read(ref _cached);
        if (cached is not null && now < _cacheExpiresAtUtc)
            return cached;

        await _gate.WaitAsync(ct);
        try
        {
            now = clock.UtcNow;
            
            cached = _cached;
            if (cached is not null && now < _cacheExpiresAtUtc)
                return cached;

            var snapshot = await ExecuteWithRetryAsync(() => BuildAsync(now, ct), "portfolio snapshot", ct);

            Volatile.Write(ref _cached, snapshot);
            
            _cacheExpiresAtUtc = now.AddMilliseconds(_options.SnapshotCacheMilliseconds);
            
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() 
        => _cacheExpiresAtUtc = DateTime.MinValue;

    private async Task<PortfolioSnapshot> BuildAsync(DateTime now, CancellationToken ct)
    {
        if (!_options.Enabled)
            return Empty(now);

        var botNames = _options.Bots.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var redisPositionsTask = LoadRedisPositionsAsync(botNames, ct);
        var paperPositionsTask = paperPositionSource.GetOpenAsync(ct);

        await Task.WhenAll(redisPositionsTask, paperPositionsTask);

        var redisPositions = await redisPositionsTask;
        var paperPositions = await paperPositionsTask;

        var symbols = redisPositions.Select(x => x.Symbol)
            .Concat(paperPositions.Select(x => x.Symbol))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var prices = await LoadPricesAsync(symbols, ct);

        var positions = redisPositions.Select(x => MapRedisPosition(x, prices[x.Symbol]))
            .Concat(paperPositions.Select(x => MapPaperPosition(x, prices[x.Symbol])))
            .GroupBy(x => new { x.BotName, x.PositionId })
            .Select(x => x.First())
            .ToArray();

        var unrealizedPnl = positions.Sum(x => x.UnrealizedPnl);
        var performance = await performanceSource.GetAsync(
            unrealizedPnl,
            _options.InitialEquity,
            now,
            ct);

        var equity = _options.InitialEquity + performance.RealizedPnlToday + unrealizedPnl;
        var peak = Math.Max(performance.PeakEquityToday, equity);
        var drawdown = Math.Max(peak - equity, 0m);
        var drawdownPercent = peak <= 0 ? 0m : drawdown / peak * 100m;

        var symbolSnapshots = positions
            .GroupBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(group => new SymbolExposureSnapshot(
                group.Key,
                group.Count(),
                group.Where(x => x.Side == PositionSide.Long).Sum(x => x.Notional),
                group.Where(x => x.Side == PositionSide.Short).Sum(x => x.Notional),
                group.Sum(x => x.Notional),
                group.Sum(x => x.SignedNotional),
                group.Sum(x => x.UnrealizedPnl)))
            .OrderBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var botSnapshots = positions.GroupBy(x => x.BotName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new BotExposureSnapshot(
                group.Key,
                group.Count(),
                group.Sum(x => x.Notional),
                group.Sum(x => x.SignedNotional),
                group.Sum(x => x.UnrealizedPnl),
                group.Sum(x => x.EstimatedInitialMargin)))
            .OrderBy(x => x.BotName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        logger.LogInformation("Portfolio snapshot built. RedisPositions = {RedisPositions}, PaperPositions = {PaperPositions}, TotalPositions = {TotalPositions}, GrossNotional = {GrossNotional}, NetNotional = {NetNotional}, Equity = {Equity}",
            redisPositions.Count,
            paperPositions.Count,
            positions.Length,
            positions.Sum(x => x.Notional),
            positions.Sum(x => x.SignedNotional),
            equity);

        return new PortfolioSnapshot(
            now,
            _options.InitialEquity,
            performance.RealizedPnlToday,
            unrealizedPnl,
            equity,
            peak,
            drawdown,
            drawdownPercent,
            performance.ConsecutiveLosses,
            positions.Length,
            positions.Sum(x => x.Notional),
            positions.Sum(x => x.SignedNotional),
            positions.Sum(x => x.EstimatedInitialMargin),
            positions,
            symbolSnapshots,
            botSnapshots);
    }

    private async Task<IReadOnlyCollection<BotPosition>> LoadRedisPositionsAsync(IReadOnlyCollection<string> botNames, CancellationToken ct)
    {
        var tasks = botNames.Select(async botName =>
        {
            var positions = await positionStore.GetAllAsync(botName, ct);
            return positions.Where(x => !x.Closed && x.RemainingQuantity > 0).ToArray();
        });

        var positionsByBot = await Task.WhenAll(tasks);
        return positionsByBot.SelectMany(x => x).ToArray();
    }

    private async Task<IReadOnlyDictionary<string, decimal>> LoadPricesAsync(IReadOnlyCollection<string> symbols, CancellationToken ct)
    {
        if (symbols.Count == 0)
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var tasks = symbols.ToDictionary(
            s => s,
            s => marketPriceProvider.GetMarkPriceAsync(s, ct),
            StringComparer.OrdinalIgnoreCase);

        await Task.WhenAll(tasks.Values);

        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (symbol, task) in tasks)
        {
            var price = await task;
            if (price <= 0)
                throw new InvalidOperationException($"Invalid mark price '{price}' for portfolio s '{symbol}'.");

            prices[symbol] = price;
        }

        return prices;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string operation, CancellationToken ct)
    {
        Exception? lastError = null;
        var attempts = _options.LoadRetryCount + 1;

        for (var i = 1; i <= attempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await action();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastError = exception;

                if (i == attempts)
                    break;

                logger.LogWarning(exception, "Transient {Operation} failure. Attempt = {Attempt}/{Attempts}. Retrying.", operation, i, attempts);

                if (_options.LoadRetryDelayMilliseconds > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(_options.LoadRetryDelayMilliseconds), ct);
            }
        }

        throw new InvalidOperationException($"Could not build {operation} after {attempts} i(s). Risk evaluation must fail closed.", lastError);
    }

    private PortfolioPositionSnapshot MapRedisPosition(BotPosition position, decimal markPrice)
    {
        var entryPrice = position.EntryPrice ?? markPrice;
        var quantity = position.RemainingQuantity;
        var notional = markPrice * quantity;
        var direction = position.Side == PositionSide.Long ? 1m : -1m;
        var unrealizedPnl = (markPrice - entryPrice) * quantity * direction;

        return new PortfolioPositionSnapshot(
            position.BotName,
            position.ShortId,
            position.Symbol,
            position.Side,
            quantity,
            entryPrice,
            markPrice,
            notional,
            notional * direction,
            unrealizedPnl,
            notional / _options.DefaultLeverage,
            position.ParentFilledAtUtc ?? position.CreatedAtUtc);
    }

    private PortfolioPositionSnapshot MapPaperPosition(PaperPortfolioPosition position, decimal markPrice)
    {
        var notional = markPrice * position.Quantity;
        var direction = position.Side == PositionSide.Long ? 1m : -1m;
        var unrealizedPnl =
            (markPrice - position.EntryPrice) * position.Quantity * direction;

        return new PortfolioPositionSnapshot(
            position.BotName,
            position.ShortId,
            position.Symbol,
            position.Side,
            position.Quantity,
            position.EntryPrice,
            markPrice,
            notional,
            notional * direction,
            unrealizedPnl,
            notional / _options.DefaultLeverage,
            position.OpenedAtUtc);
    }

    private PortfolioSnapshot Empty(DateTime now) => new(
        now,
        _options.InitialEquity,
        0m,
        0m,
        _options.InitialEquity,
        _options.InitialEquity,
        0m,
        0m,
        0,
        0,
        0m,
        0m,
        0m,
        [],
        [],
        []);
}
