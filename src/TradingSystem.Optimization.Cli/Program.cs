using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Backtesting.Bots;
using TradingSystem.Backtesting.Bots.Bot8012;
using TradingSystem.Backtesting.Bots.Signals;
using TradingSystem.HistoricalData;
using TradingSystem.Optimization;
using TradingSystem.Optimization.Engine;
using TradingSystem.Optimization.Models;
using TradingSystem.Optimization.ParameterSpaces;

var map  =  Parse(args);
var services  =  new ServiceCollection().AddBotBacktesting().AddTradingOptimization();
using var provider  =  services.BuildServiceProvider();
var symbol  =  Get("symbol",  "BTCUSDC");
var interval  =  Get("interval",  "1m");
var candles  =  await new CsvHistoricalCandleSource(Get("candles",  "data/BTCUSDC_1m.csv"))
    .LoadAsync(symbol,  interval,  null,  null);
var signalPath  =  Get("signals",  string.Empty);
var signals  =  File.Exists(signalPath)
    ? await new CsvHistoricalBotSignalSource(signalPath).LoadAsync()
    : new EmaCrossDemoSignalSource().Generate(candles);

var engine  =  provider.GetRequiredService<Bot8012BacktestEngine>();
var tuning  =  provider.GetRequiredService<ParameterTuningEngine>();
var walkForward  =  provider.GetRequiredService<WalkForwardOptimizationEngine>();
var seed  =  new Bot8012BacktestOptions { Symbol  =  symbol,  InitialBalance  =  Decimal("balance",  10_000m) };
var candidates  =  BotParameterSpaces.Bot8012(seed).ToArray();
var weights  =  new OptimizationScoreWeights();

if (map.ContainsKey("walk-forward"))
{
    var result  =  await walkForward.RunAsync(
        "BOT8012",  candles,  signals,  candidates, 
        (o,  c,  s,  _)  =>  Task.FromResult(engine.Run(c,  s,  o)), 
        x  =>  x.Metrics, 
        new WalkForwardOptions
        {
            TrainingBars  =  Int("train-bars",  10_000), 
            TestingBars  =  Int("test-bars",  2_000), 
            StepBars  =  Int("step-bars",  2_000), 
            AnchoredTraining  =  map.ContainsKey("anchored")
        },  weights);
    Console.WriteLine($"Windows = {result.Windows.Count},  Average OOS score = {result.AverageOutOfSampleScore:F2},  OOS net = {result.TotalOutOfSampleNetProfit:F2},  Worst DD = {result.WorstOutOfSampleDrawdownPercent:F2}%");
    foreach (var w in result.Windows)
        Console.WriteLine($"#{w.WindowNumber} train = {w.TrainFromUtc:d}-{w.TrainToUtc:d} test = {w.TestFromUtc:d}-{w.TestToUtc:d} IS = {w.InSampleScore:F2} OOS = {w.OutOfSampleScore:F2}");
}
else
{
    var rows  =  await tuning.RunAsync(candidates,  (o,  _)  =>  Task.FromResult(engine.Run(candles,  signals,  o)),  x  =>  x.Metrics,  weights,  Int("top",  20));
    foreach (var row in rows)
        Console.WriteLine($"Score = {row.Score:F2} Net = {row.Metrics.NetProfit:F2} DD = {row.Metrics.MaximumDrawdownPercent:F2}% Options = {row.Options}");
}

string Get(string key,  string fallback)  =>  map.TryGetValue(key,  out var value) ? value : fallback;
int Int(string key,  int fallback)  =>  int.TryParse(Get(key,  string.Empty),  out var value) ? value : fallback;
decimal Decimal(string key,  decimal fallback)  =>  decimal.TryParse(Get(key,  string.Empty),  System.Globalization.NumberStyles.Any,  System.Globalization.CultureInfo.InvariantCulture,  out var value) ? value : fallback;
static Dictionary<string, string> Parse(string[] values) { var result  =  new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); for (var i = 0;i<values.Length;i++) { if (!values[i].StartsWith("--")) continue; var key = values[i][2..]; var value = i+1<values.Length && !values[i+1].StartsWith("--")?values[++i]:"true"; result[key] = value; } return result; }
