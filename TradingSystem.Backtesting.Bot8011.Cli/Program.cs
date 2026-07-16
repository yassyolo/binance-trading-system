using TradingSystem.Backtesting.Bot8011.Engine;
using TradingSystem.Backtesting.Bot8011.Models;
using TradingSystem.Backtesting.Bot8011.Optimization;
using TradingSystem.Backtesting.Bot8011.Reporting;
using TradingSystem.Backtesting.Bot8011.Signals;
using TradingSystem.HistoricalData;

var a = Parse(args);
var candleFile = Get(a, "candles", "samples/BTCUSDC_1h_sample.csv");
var signalFile = Get(a, "signals", "samples/bot8011/signals.csv");
var output = Get(a, "output", "output/bot8011");
var initial = decimal.Parse(Get(a, "balance", "10000"), System.Globalization.CultureInfo.InvariantCulture);
var csv = new CsvHistoricalCandleSource(candleFile);
var candles = await csv.LoadAsync(Get(a, "symbol", "BTCUSDC"), Get(a, "interval", "1h"), null, null);
IBot8011SignalSource source = File.Exists(signalFile) ? new CsvBot8011SignalSource(signalFile) : new EmaCrossDemoSignalSource();
var signals = await source.LoadAsync(candles);
var baseline = new Bot8011BacktestOptions { Symbol = Get(a, "symbol", "BTCUSDC") };
var engine = new Bot8011BacktestEngine();
var writer = new Bot8011ReportWriter();

if (a.ContainsKey("optimize"))
{
    var optimizer = new Bot8011Optimizer(engine);
    var rows = await optimizer.RunAsync(initial, baseline, new Bot8011OptimizationSpace(), candles, signals, top: 100);
    var path = Path.Combine(output, "optimization.csv");
    await writer.WriteOptimizationCsvAsync(rows, path);
    Console.WriteLine($"Optimization completed. Combinations ranked at: {Path.GetFullPath(path)}");
    foreach (var row in rows.Take(10)) Console.WriteLine($"Score={row.Score:F2} TP={row.Options.TakeProfitPercent}% SL={row.Options.StopLossPercent}% Step={row.Options.Stop3TrailingStep} Buffer={row.Options.Stop3TrailingBuffer} DD={row.Metrics.MaximumDrawdownPercent:F2}% Net={row.Metrics.NetProfit:F2}");
}
else
{
    var result = await engine.RunAsync(initial, baseline, candles, signals);
    var dir = await writer.WriteAsync(result, output);
    Console.WriteLine($"BOT8011 backtest complete: {Path.GetFullPath(dir)}");
    Console.WriteLine($"Positions={result.Metrics.Positions}, Net={result.Metrics.NetProfit:F2}, PF={result.Metrics.ProfitFactor:F2}, DD={result.Metrics.MaximumDrawdownPercent:F2}%");
}

static Dictionary<string, string> Parse(string[] args)
{
    var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++) if (args[i].StartsWith("--")) { var key = args[i][2..]; var value = i + 1 < args.Length && !args[i + 1].StartsWith("--") ? args[++i] : "true"; d[key] = value; }
    return d;
}
static string Get(Dictionary<string, string> d, string key, string fallback) => d.TryGetValue(key, out var v) ? v : fallback;
