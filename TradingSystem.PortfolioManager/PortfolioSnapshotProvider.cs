using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Application.Engine;
using TradingSystem.Application.Positions;
using TradingSystem.Application.Time;
using TradingSystem.Domain.Enums;
using TradingSystem.Domain.Positions;

namespace TradingSystem.PortfolioManager;

public sealed class PortfolioSnapshotProvider(
    IOptions<PortfolioOptions> options,
    IPositionStore positionStore,
    IMarketPriceProvider marketPriceProvider,
    IPortfolioPerformanceSource performanceSource,
    IClock clock,
    ILogger<PortfolioSnapshotProvider> logger) : IPortfolioSnapshotProvider
{
    private readonly PortfolioOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private PortfolioSnapshot? _cached;
    private DateTime _cacheExpiresAtUtc;

    public async Task<PortfolioSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var cached = Volatile.Read(ref _cached);
        if (cached is not null && now < _cacheExpiresAtUtc)
            return cached;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            now = clock.UtcNow;
            cached = _cached;
            if (cached is not null && now < _cacheExpiresAtUtc)
                return cached;

            var snapshot = await BuildAsync(now, cancellationToken);
            _cached = snapshot;
            _cacheExpiresAtUtc = now.AddMilliseconds(_options.SnapshotCacheMilliseconds);
            return snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate()
    {
        _cacheExpiresAtUtc = DateTime.MinValue;
    }

    private async Task<PortfolioSnapshot> BuildAsync(DateTime now, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return Empty(now);

        var botNames = _options.Bots
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var positionTasks = botNames.Select(async botName =>
        {
            var positions = await positionStore.GetAllAsync(botName, cancellationToken);
            return positions.Where(x => !x.Closed && x.RemainingQuantity > 0).ToArray();
        });

        var openPositions = (await Task.WhenAll(positionTasks))
            .SelectMany(x => x)
            .ToArray();

        var symbols = openPositions
            .Select(x => x.Symbol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var priceTasks = symbols.ToDictionary(
            symbol => symbol,
            symbol => marketPriceProvider.GetMarkPriceAsync(symbol, cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        await Task.WhenAll(priceTasks.Values);

        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (symbol, task) in priceTasks)
        {
            var price = await task;
            if (price <= 0)
                throw new InvalidOperationException($"Invalid mark price '{price}' for portfolio symbol '{symbol}'.");
            prices[symbol] = price;
        }

        var positions = openPositions
            .Select(position => Map(position, prices[position.Symbol]))
            .ToArray();

        var unrealizedPnl = positions.Sum(x => x.UnrealizedPnl);
        var performance = await performanceSource.GetAsync(
            unrealizedPnl,
            _options.InitialEquity,
            now,
            cancellationToken);

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

        var botSnapshots = positions
            .GroupBy(x => x.BotName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new BotExposureSnapshot(
                group.Key,
                group.Count(),
                group.Sum(x => x.Notional),
                group.Sum(x => x.SignedNotional),
                group.Sum(x => x.UnrealizedPnl),
                group.Sum(x => x.EstimatedInitialMargin)))
            .OrderBy(x => x.BotName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        logger.LogDebug(
            "Portfolio snapshot built. Positions = {Positions}, GrossNotional = {GrossNotional}, Equity = {Equity}",
            positions.Length,
            positions.Sum(x => x.Notional),
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

    private PortfolioPositionSnapshot Map(BotPosition position, decimal markPrice)
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
