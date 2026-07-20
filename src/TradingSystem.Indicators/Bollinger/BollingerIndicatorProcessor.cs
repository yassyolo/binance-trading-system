using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TradingSystem.Contracts.Indicators;
using TradingSystem.Domain.MarketData;
using TradingSystem.Indicators.Abstractions;
using TradingSystem.Indicators.Common;
namespace TradingSystem.Indicators.Bollinger;
public sealed class BollingerIndicatorProcessor(IOptions<BollingerOptions> options):IIndicatorProcessor
{
 readonly BollingerOptions _o = options.Value; readonly ConcurrentDictionary<string, State> _states = new();
 public string Name => "bb"; public IReadOnlyCollection<string> Symbols => _o.Symbols; public IReadOnlyCollection<string> Intervals => _o.Intervals; public int RequiredHistory => Math.Max(_o.HistoryLimit, _o.Bands.Max(x => x.Length));
 public void Initialize(string symbol, string interval, IReadOnlyList<MarketCandle> candles){var s = new State(_o);foreach(var c in candles.OrderBy(x => x.CloseTimeUtc).TakeLast(_o.HistoryLimit))s.Add(c);_states[Key(symbol, interval)] = s;}
 public IndicatorSnapshotMessage? Process(MarketCandle c, DateTimeOffset now){if(!c.IsClosed || !_states.TryGetValue(Key(c.Symbol, c.Interval), out var s))return null;var values = s.Add(c);if(values is null)return null;return new(){Type = Name, Symbol = c.Symbol, Timeframe = c.Interval, CandleOpenTime = new DateTimeOffset(c.OpenTimeUtc).ToUnixTimeMilliseconds(), CandleCloseTime = new DateTimeOffset(c.CloseTimeUtc).ToUnixTimeMilliseconds(), PublishedAt = now.ToUnixTimeMilliseconds(), Indicators = values};}
 static string Key(string s, string i) => $"{s.ToUpperInvariant()}:{i.ToLowerInvariant()}";
 sealed class State{readonly BollingerOptions o;readonly List<MarketCandle> q = [];DateTime last;Dictionary<string, (decimal b, decimal u, decimal l)> previous = [];public State(BollingerOptions o) => this.o = o;
 public IReadOnlyDictionary<string, IndicatorValueMessage>? Add(MarketCandle c){if(c.CloseTimeUtc<=last)return null;last = c.CloseTimeUtc;q.Add(c);if(q.Count>o.HistoryLimit)q.RemoveRange(0, q.Count-o.HistoryLimit);var result = new Dictionary<string, IndicatorValueMessage>();var current = new Dictionary<string, (decimal, decimal, decimal)>();foreach(var b in o.Bands){var vals = q.Select(x => b.Source.Equals("open", StringComparison.OrdinalIgnoreCase)?x.Open:x.Close).TakeLast(b.Length).ToArray();if(vals.Length<b.Length)return null;var basis = vals.Average();var dev = b.Multiplier*IndicatorMath.StandardDeviation(vals);var upper = basis+dev;var lower = basis-dev;previous.TryGetValue(b.Name, out var p);result[$"{b.Name}.basis"] = new(){Value = basis, PreviousValue = p.b, Metadata = Meta(b)};result[$"{b.Name}.upper"] = new(){Value = upper, PreviousValue = p.u, Metadata = Meta(b)};result[$"{b.Name}.lower"] = new(){Value = lower, PreviousValue = p.l, Metadata = Meta(b)};current[b.Name] = (basis, upper, lower);}previous = current;return result;}
 static IReadOnlyDictionary<string, string> Meta(BollingerBandOptions b) => new Dictionary<string, string>{{"length", b.Length.ToString()}, {"source", b.Source}, {"ma_type", "SMA"}, {"multiplier", b.Multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)}};}
}

