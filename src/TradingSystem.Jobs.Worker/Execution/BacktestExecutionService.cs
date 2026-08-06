using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Analytics.Abstractions;
using TradingSystem.Backtesting.Bots.Bot8011;
using TradingSystem.Backtesting.Bots.Bot8012;
using TradingSystem.Backtesting.Bots.Bot8013;
using TradingSystem.Backtesting.Bots.Bot8014;
using TradingSystem.Backtesting.Bots.Bot8015;
using TradingSystem.Backtesting.Bots.Bot8016;
using TradingSystem.Backtesting.Bots.Common;
using TradingSystem.Backtesting.Bots.Signals;
using TradingSystem.Dashboard.Contracts;
using TradingSystem.JobOrchestration;
using TradingSystem.Optimization.Analytics;

namespace TradingSystem.Jobs.Worker.Execution;

public sealed class BacktestExecutionService(
    IServiceProvider services,
    IHistoricalMarketDataStore market,
    IHistoricalSignalStore signalStore,
    IPerformanceAnalyticsStore analytics)
{
    private static readonly JsonSerializerOptions OptionJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async Task<Guid> ExecuteAsync(
        BacktestRequest request,
        string interval,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BotName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(interval);

        if (request.ToUtc <= request.FromUtc)
            throw new ArgumentException("Backtest ToUtc must be after FromUtc.");

        var fromUtc = NormalizeUtc(request.FromUtc);
        var toUtc = NormalizeUtc(request.ToUtc);

        if (await market.HasGapsAsync(
                request.Symbol,
                interval,
                fromUtc,
                toUtc,
                ct))
        {
            throw new InvalidOperationException(
                "Historical data contains unresolved candle gaps for the requested period.");
        }

        var candles = await market.LoadCandlesAsync(
            request.Symbol,
            interval,
            fromUtc,
            toUtc,
            ct);

        if (candles.Count < 2)
            throw new InvalidOperationException(
                "Historical candles are missing for the requested period.");

        var signals = string.Equals(
            request.SignalSource,
            "Internal",
            StringComparison.OrdinalIgnoreCase)
            ? new EmaCrossDemoSignalSource().Generate(candles)
            : await signalStore.LoadAsync(
                request.BotName,
                request.Symbol,
                fromUtc,
                toUtc,
                ct);

        if (signals.Count == 0)
            throw new InvalidOperationException(
                "No historical signals were found for the requested source and period.");

        var parameters = request.Parameters
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return request.BotName.Trim().ToUpperInvariant() switch
        {
            "BOT8011" => await SaveAsync(
                await Get<Bot8011BacktestEngine>().RunAsync(
                    request.InitialBalance,
                    ApplyOptions(
                        new Bot8011BacktestOptions
                        {
                            Symbol = request.Symbol
                        },
                        parameters),
                    candles,
                    signals,
                    ct),
                "1.0.0",
                interval,
                ct),

            "BOT8012" => await SaveAsync(
                Get<Bot8012BacktestEngine>().Run(
                    candles,
                    signals,
                    ApplyOptions(
                        new Bot8012BacktestOptions
                        {
                            Symbol = request.Symbol,
                            InitialBalance = request.InitialBalance,
                            EntryFeeRate = request.CommissionPercent / 100m,
                            ExitFeeRate = request.CommissionPercent / 100m,
                            SlippageBasisPoints = request.SlippagePercent * 100m
                        },
                        parameters)),
                "1.0.0",
                interval,
                ct),

            "BOT8013" => await SaveAsync(
                Get<Bot8013BacktestEngine>().Run(
                    candles,
                    signals,
                    ApplyOptions(
                        new Bot8013BacktestOptions
                        {
                            Symbol = request.Symbol,
                            InitialBalance = request.InitialBalance
                        },
                        parameters)),
                "1.0.0",
                interval,
                ct),

            "BOT8014" => await SaveAsync(
                Get<Bot8014BacktestEngine>().Run(
                    candles,
                    signals,
                    ApplyOptions(
                        new Bot8014BacktestOptions
                        {
                            Symbol = request.Symbol,
                            InitialBalance = request.InitialBalance
                        },
                        parameters)),
                "1.0.0",
                interval,
                ct),

            "BOT8015" => await SaveAsync(
                await Get<Bot8015BacktestEngine>().RunAsync(
                    candles,
                    signals,
                    ApplyOptions(
                        new Bot8015BacktestOptions
                        {
                            Symbol = request.Symbol,
                            InitialBalance = request.InitialBalance
                        },
                        parameters),
                    ct),
                "1.0.0",
                interval,
                ct),

            "BOT8016" => await SaveAsync(
                Get<Bot8016BacktestEngine>().Run(
                    candles,
                    BuildIndicators(candles),
                    ApplyOptions(
                        new Bot8016BacktestOptions
                        {
                            Symbol = request.Symbol,
                            EntryTimeframe = interval,
                            ExitTimeframe = interval,
                            InitialBalance = request.InitialBalance
                        },
                        parameters)),
                "1.0.0",
                interval,
                ct),

            _ => throw new NotSupportedException(
                "Supported bots are BOT8011-BOT8016.")
        };
    }

    private T Get<T>() where T : notnull =>
        services.GetRequiredService<T>();

    private async Task<Guid> SaveAsync<T>(
        BotBacktestResult<T> result,
        string version,
        string interval,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(result);

        var mapped = BacktestPerformanceMapper.Map(
            result,
            version,
            interval);

        await analytics.SaveCompletedBacktestAsync(
            mapped.Run,
            mapped.Snapshot,
            mapped.Trades,
            ct);

        return mapped.Run.RunId;
    }

    private static T ApplyOptions<T>(
        T seed,
        IReadOnlyDictionary<string, string> values)
    {
        var json = JsonSerializer.SerializeToNode(seed)?.AsObject()
            ?? throw new InvalidOperationException(
                $"Could not serialize options of type {typeof(T).Name}.");

        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;

            var key = json
                .Select(x => x.Key)
                .FirstOrDefault(x =>
                    x.Equals(
                        pair.Key,
                        StringComparison.OrdinalIgnoreCase));

            if (key is null)
                continue;

            json[key] = JsonValue.Create(pair.Value);
        }

        return JsonSerializer.Deserialize<T>(
                   json.ToJsonString(),
                   OptionJsonOptions)
               ?? throw new InvalidOperationException(
                   $"Could not deserialize options of type {typeof(T).Name}.");
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static IReadOnlyList<HistoricalAlligatorSnapshot> BuildIndicators(
        IReadOnlyList<TradingSystem.Domain.MarketData.MarketCandle> candles)
    {
        var rows = new List<HistoricalAlligatorSnapshot>(candles.Count);

        for (var i = 0; i < candles.Count; i++)
        {
            var start = Math.Max(0, i - 199);
            var average = candles
                .Skip(start)
                .Take(i - start + 1)
                .Average(x => x.Close);

            rows.Add(new HistoricalAlligatorSnapshot(
                candles[i].CloseTimeUtc,
                candles[i].Symbol,
                candles[i].Interval,
                average,
                average,
                average,
                average));
        }

        return rows;
    }
}
