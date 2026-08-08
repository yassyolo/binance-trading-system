using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Application.MarketData;
using TradingSystem.Backtesting;
using TradingSystem.Backtesting.Engine;
using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Models.Enums;
using TradingSystem.Backtesting.Reporting.Writers;
using TradingSystem.Backtesting.Strategies;
using TradingSystem.Binance.Market;
using TradingSystem.HistoricalData;

var arguments  =  CliArguments.Parse(args);
var services  =  new ServiceCollection();
services.AddBacktesting();
services.AddSingleton<BacktestReportWriter>();
using var provider  =  services.BuildServiceProvider();

if (arguments.Has("list-strategies"))
{
    Console.WriteLine(string.Join(Environment.NewLine, 
        provider.GetRequiredService<BacktestStrategyRegistry>().Names));
    return;
}

var strategy  =  arguments.Get("strategy",  "EMA_CROSS");
var symbol  =  arguments.Get("symbol",  "BTCUSDC").ToUpperInvariant();
var interval  =  arguments.Get("interval",  "1h").ToLowerInvariant();
var sourceName  =  arguments.Get("source",  "csv");
var fromUtc  =  arguments.GetUtcDate("from");
var toUtc  =  arguments.GetUtcDate("to");

using var httpClient  =  new HttpClient
{
    BaseAddress  =  new Uri("https://fapi.binance.com"), 
    Timeout  =  TimeSpan.FromSeconds(30)
};

IHistoricalCandleRangeSource source  =  sourceName.Equals("binance",  StringComparison.OrdinalIgnoreCase)
    ? new BinanceHistoricalCandleRangeSource(httpClient)
    : new CsvHistoricalCandleSource(arguments.Get("file",  "samples/BTCUSDC_1h_sample.csv"));

Console.WriteLine($"Loading {symbol} {interval} candles from {sourceName}...");
var candles  =  await source.LoadAsync(symbol,  interval,  fromUtc,  toUtc);
Console.WriteLine($"Loaded {candles.Count:N0} candles.");

var strategyParameters  =  arguments.Values
    .Where(pair  =>  pair.Key.StartsWith("param.",  StringComparison.OrdinalIgnoreCase))
    .ToDictionary(pair  =>  pair.Key[6..],  pair  =>  pair.Value,  StringComparer.OrdinalIgnoreCase);

var request  =  new BacktestRequest
{
    StrategyName  =  strategy, 
    Symbol  =  symbol, 
    Interval  =  interval, 
    StartUtc  =  fromUtc, 
    EndUtc  =  toUtc, 
    InitialBalance  =  arguments.GetDecimal("balance",  10_000m), 
    RiskPerTradePercent  =  arguments.GetDecimal("risk",  1m), 
    SlippageBasisPoints  =  arguments.GetDecimal("slippage-bps",  1m), 
    EntryExecutionMode  =  arguments.GetEnum("entry-mode",  EntryExecutionMode.NextCandleOpen), 
    ConflictPolicy  =  arguments.GetEnum("conflict-policy",  IntrabarConflictPolicy.WorstCase), 
    StrategyParameters  =  strategyParameters
};

var rules  =  new SymbolTradingRules
{
    TickSize  =  arguments.GetDecimal("tick-size",  0.1m), 
    QuantityStep  =  arguments.GetDecimal("quantity-step",  0.001m), 
    MinimumQuantity  =  arguments.GetDecimal("min-quantity",  0.001m), 
    MaximumQuantity  =  arguments.GetDecimal("max-quantity",  1_000m), 
    MinimumNotional  =  arguments.GetDecimal("min-notional",  5m), 
    ContractMultiplier  =  arguments.GetDecimal("contract-multiplier",  1m), 
    Leverage  =  arguments.GetInt("leverage",  20)
};

var result  =  await provider.GetRequiredService<BacktestEngine>()
    .RunAsync(request,  candles,  rules);
var output  =  await provider.GetRequiredService<BacktestReportWriter>()
    .WriteAllAsync(result,  arguments.Get("output",  "output"));

Console.WriteLine($"Final balance: {result.FinalBalance:F2}");
Console.WriteLine($"Net profit: {result.Metrics.NetProfit:F2}");
Console.WriteLine($"Trades: {result.Metrics.TotalTrades}; Win rate: {result.Metrics.WinRatePercent:F2}%");
Console.WriteLine($"Reports: {Path.GetFullPath(output)}");

internal sealed class CliArguments
{
    private CliArguments(Dictionary<string,  string> values)  =>  Values  =  values;
    public IReadOnlyDictionary<string,  string> Values { get; }
    public bool Has(string key)  =>  Values.ContainsKey(key);
    public string Get(string key,  string fallback)  =>  Values.TryGetValue(key,  out var value) ? value : fallback;
    public int GetInt(string key,  int fallback)  =>  int.TryParse(Get(key,  ""),  NumberStyles.Integer,  CultureInfo.InvariantCulture,  out var value) ? value : fallback;
    public decimal GetDecimal(string key,  decimal fallback)  =>  decimal.TryParse(Get(key,  ""),  NumberStyles.Any,  CultureInfo.InvariantCulture,  out var value) ? value : fallback;
    public TEnum GetEnum<TEnum>(string key,  TEnum fallback) where TEnum : struct,  Enum
         =>  Enum.TryParse<TEnum>(Get(key,  ""),  true,  out var value) ? value : fallback;
    public DateTime? GetUtcDate(string key)
    {
        if (!DateTime.TryParse(Get(key,  ""),  CultureInfo.InvariantCulture, 
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,  out var value))
            return null;
        return DateTime.SpecifyKind(value,  DateTimeKind.Utc);
    }
    public static CliArguments Parse(string[] args)
    {
        var values  =  new Dictionary<string,  string>(StringComparer.OrdinalIgnoreCase);
        for (var index  =  0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--",  StringComparison.Ordinal))
                continue;
            var key  =  args[index][2..];
            var value  =  index + 1 < args.Length  &&  !args[index + 1].StartsWith("--",  StringComparison.Ordinal)
                ? args[++index]
                : "true";
            values[key]  =  value;
        }
        return new CliArguments(values);
    }
}
