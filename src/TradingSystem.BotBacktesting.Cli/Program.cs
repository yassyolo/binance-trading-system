using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Backtesting.Bots;
using TradingSystem.Backtesting.Bots.Bot8011;
using TradingSystem.Backtesting.Bots.Bot8012;
using TradingSystem.Backtesting.Bots.Bot8013;
using TradingSystem.Backtesting.Bots.Bot8014;
using TradingSystem.Backtesting.Bots.Bot8015;
using TradingSystem.Backtesting.Bots.Bot8016;
using TradingSystem.Backtesting.Bots.Configuration;
using TradingSystem.Backtesting.Bots.Signals;
using TradingSystem.Backtesting.Reporting.Writers;
using TradingSystem.HistoricalData;

var a = Parse(args);

var services = new ServiceCollection();

services.AddBotBacktesting();
services.AddSingleton<BotBacktestReportWriter>();

using var provider = services.BuildServiceProvider();

var bot = Get("bot", "BOT8011").ToUpperInvariant();
var symbol = Get("symbol", "BTCUSDC");
var interval = Get("interval", "1h");
var source = new CsvHistoricalCandleSource(Get("candles", "samples/BTCUSDC_1h_sample.csv"));
var candles = await source.LoadAsync(symbol, interval, Date("from"), Date("to"));
var signalPath = Get("signals", "");
var signals = File.Exists(signalPath)
    ? await new CsvHistoricalBotSignalSource(signalPath).LoadAsync()
    : new EmaCrossDemoSignalSource(Int("ema-fast", 20), Int("ema-slow", 50)).Generate(candles);
var writer = provider.GetRequiredService<BotBacktestReportWriter>();
var output = Get("output", "output/bots");

object result = bot switch
{
    "BOT8011" => await provider.GetRequiredService<Bot8011BacktestEngine>().RunAsync(Decimal("balance", 10000m), new Bot8011BacktestOptions { Symbol = symbol }, candles, signals),
    "BOT8012" => provider.GetRequiredService<Bot8012BacktestEngine>().Run(candles, signals, new Bot8012BacktestOptions { Symbol = symbol, InitialBalance = Decimal("balance", 10000m) }),
    "BOT8013" => provider.GetRequiredService<Bot8013BacktestEngine>().Run(candles, signals, new Bot8013BacktestOptions { Symbol = symbol, InitialBalance = Decimal("balance", 10000m) }),
    "BOT8014" => provider.GetRequiredService<Bot8014BacktestEngine>().Run(candles, signals, new Bot8014BacktestOptions { Symbol = symbol, InitialBalance = Decimal("balance", 10000m) }),
    "BOT8015" => await provider.GetRequiredService<Bot8015BacktestEngine>().RunAsync(candles, signals, new Bot8015BacktestOptions { Symbol = symbol, InitialBalance = Decimal("balance", 10000m) }),
    "BOT8016" => provider.GetRequiredService<Bot8016BacktestEngine>().Run(candles, BuildIndicators(candles), new Bot8016BacktestOptions { Symbol = symbol, EntryTimeframe = interval, ExitTimeframe = interval, InitialBalance = Decimal("balance", 10000m) }),
    _ => throw new ArgumentException("--bot must be BOT8011-BOT8016")
};
var dir = await Write(result); Console.WriteLine($"{bot} report: {Path.GetFullPath(dir)}");
async Task<string> Write(object x) => x switch { TradingSystem.Backtesting.Bots.Models.BotBacktestResult<Bot8011BacktestOptions> r 
    => await writer.WriteAsync(r, output), TradingSystem.Backtesting.Bots.Models.BotBacktestResult<Bot8012BacktestOptions> r 
    => await writer.WriteAsync(r, output), TradingSystem.Backtesting.Bots.Models.BotBacktestResult<Bot8013BacktestOptions> r 
    => await writer.WriteAsync(r, output), TradingSystem.Backtesting.Bots.Models.BotBacktestResult<Bot8014BacktestOptions> r 
    => await writer.WriteAsync(r, output), TradingSystem.Backtesting.Bots.Models.BotBacktestResult<Bot8015BacktestOptions> r 
    => await writer.WriteAsync(r, output), TradingSystem.Backtesting.Bots.Models.BotBacktestResult<Bot8016BacktestOptions> r 
    => await writer.WriteAsync(r, output), _ => throw new InvalidOperationException() };
static IReadOnlyList<HistoricalAlligatorSnapshot> BuildIndicators(IReadOnlyList<TradingSystem.Domain.MarketData.MarketCandle> candles) { var r = new List<HistoricalAlligatorSnapshot>(); for (var i = 0; i < candles.Count; i++) { var start = Math.Max(0, i - 199); var avg = candles.Skip(start).Take(i - start + 1).Average(x => x.Close); r.Add(new(candles[i].CloseTimeUtc, candles[i].Symbol, candles[i].Interval, avg, avg, avg, avg)); } return r; }
string Get(string k, string f) => a.TryGetValue(k, out var v) ? v : f; int Int(string k, int f) => int.TryParse(Get(k, ""), out var v) ? v : f; decimal Decimal(string k, decimal f) => decimal.TryParse(Get(k, ""), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : f; DateTime? Date(string k) => DateTime.TryParse(Get(k, ""), out var v) ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null; static Dictionary<string, string> Parse(string[] x) { var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); for (var i = 0; i < x.Length; i++) { if (!x[i].StartsWith("--")) continue; var k = x[i][2..]; var v = i + 1 < x.Length && !x[i + 1].StartsWith("--") ? x[++i] : "true"; d[k] = v; } return d; }