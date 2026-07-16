using TradingSystem.Backtesting.Bot8012.Data;
using TradingSystem.Backtesting.Bot8012.Engine;
using TradingSystem.Backtesting.Bot8012.Models;
using TradingSystem.Backtesting.Bot8012.Optimization;
using TradingSystem.Backtesting.Bot8012.Reporting;

var mode = args.FirstOrDefault()?.ToLowerInvariant() ?? "backtest";
string Get(string name, string fallback) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
var candles = CsvLoaders.Candles(Get("--candles", "samples/BTCUSDC_1m.csv"));
var signals = CsvLoaders.Signals(Get("--signals", "samples/BOT8012_signals.csv"));
var output = Get("--output", "output/bot8012");
var options = new Bot8012BacktestOptions();
if (mode == "optimize") { var rows = new Bot8012Optimizer().Run(candles, signals, options); await BacktestReportWriter.WriteOptimizationAsync(rows, output); Console.WriteLine($"Optimization complete. Best score={rows[0].Score:F2}; output={output}"); }
else { var result = new Bot8012BacktestEngine().Run(candles, signals, options); await BacktestReportWriter.WriteAsync(result, output); Console.WriteLine($"Backtest complete. Net={result.Metrics.NetProfit:F2}; output={output}"); }
