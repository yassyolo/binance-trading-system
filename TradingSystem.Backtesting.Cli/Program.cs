using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Backtesting;
using TradingSystem.Backtesting.Engine;
using TradingSystem.Backtesting.Models;
using TradingSystem.Backtesting.Reporting;
using TradingSystem.Backtesting.Strategies;
using TradingSystem.HistoricalData;

var argsMap = ParseArgs(args);
var services = new ServiceCollection();
services.AddBacktesting();
services.AddSingleton<BacktestReportWriter>();
using var provider = services.BuildServiceProvider();

if (argsMap.ContainsKey("list-strategies"))
{
    Console.WriteLine(string.Join(Environment.NewLine, provider.GetRequiredService<BacktestStrategyRegistry>().Names));
    return;
}

var strategy = Get("strategy", "EMA_CROSS");
var symbol = Get("symbol", "BTCUSDC");
var interval = Get("interval", "1h");
var source = Get("source", "csv");
var from = Date("from"); var to = Date("to");
IHistoricalCandleSource candleSource = source.Equals("binance", StringComparison.OrdinalIgnoreCase)
    ? new BinanceFuturesHistoricalCandleSource(new HttpClient())
    : new CsvHistoricalCandleSource(Get("file", "samples/BTCUSDC_1h_sample.csv"));

Console.WriteLine($"Loading {symbol} {interval} candles from {source}...");
var candles = await candleSource.LoadAsync(symbol, interval, from, to);
Console.WriteLine($"Loaded {candles.Count:N0} candles.");

var parameters = argsMap.Where(x => x.Key.StartsWith("param.", StringComparison.OrdinalIgnoreCase))
    .ToDictionary(x => x.Key[6..], x => x.Value, StringComparer.OrdinalIgnoreCase);
var request = new BacktestRequest
{
    StrategyName = strategy,
    Symbol = symbol,
    Interval = interval,
    StartUtc = from,
    EndUtc = to,
    InitialBalance = Decimal("balance", 10_000m),
    RiskPerTradePercent = Decimal("risk", 1m),
    SlippageBasisPoints = Decimal("slippage-bps", 1m),
    StrategyParameters = parameters
};
var rules = new SymbolTradingRules
{
    TickSize = Decimal("tick-size", 0.1m),
    QuantityStep = Decimal("quantity-step", 0.001m),
    MinimumQuantity = Decimal("min-quantity", 0.001m),
    MinimumNotional = Decimal("min-notional", 5m),
    Leverage = Int("leverage", 20)
};
var result = await provider.GetRequiredService<BacktestEngine>().RunAsync(request, candles, rules);
var output = await provider.GetRequiredService<BacktestReportWriter>().WriteAllAsync(result, Get("output", "output"));
Console.WriteLine($"Final balance: {result.FinalBalance:F2}");
Console.WriteLine($"Net profit: {result.Metrics.NetProfit:F2}");
Console.WriteLine($"Trades: {result.Metrics.TotalTrades}; Win rate: {result.Metrics.WinRatePercent:F2}%");
Console.WriteLine($"Reports: {Path.GetFullPath(output)}");

string Get(string key, string fallback) => argsMap.TryGetValue(key, out var value) ? value : fallback;
decimal Decimal(string key, decimal fallback) => decimal.TryParse(Get(key, ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
int Int(string key, int fallback) => int.TryParse(Get(key, ""), out var v) ? v : fallback;
DateTime? Date(string key) => DateTime.TryParse(Get(key, ""), out var v) ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null;
static Dictionary<string, string> ParseArgs(string[] values) { var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); for (var i = 0; i < values.Length; i++) { if (!values[i].StartsWith("--")) continue; var k = values[i][2..]; var v = i + 1 < values.Length && !values[i + 1].StartsWith("--") ? values[++i] : "true"; d[k] = v; } return d; }
