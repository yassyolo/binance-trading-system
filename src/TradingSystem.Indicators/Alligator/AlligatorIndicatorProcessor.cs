using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TradingSystem.Contracts.Indicators;
using TradingSystem.Domain.MarketData;
using TradingSystem.Indicators.Abstractions;
using TradingSystem.Indicators.Common;
namespace TradingSystem.Indicators.Alligator;
public sealed class AlligatorIndicatorProcessor(IOptions<AlligatorOptions> options) : IIndicatorProcessor
{
 private readonly AlligatorOptions _o = options.Value; private readonly ConcurrentDictionary<string, State> _states = new();
 public string Name => "alligator_ma"; public IReadOnlyCollection<string> Symbols => _o.Symbols; public IReadOnlyCollection<string> Intervals => _o.Intervals; public int RequiredHistory => Math.Max(_o.HistoryLimit, _o.SmaLength);
 public void Initialize(string symbol, string interval, IReadOnlyList<MarketCandle> candles){var s = new State(_o);foreach(var c in candles.Where(x => x.IsClosed).OrderBy(x => x.CloseTimeUtc).TakeLast(_o.HistoryLimit))s.Add(c);_states[Key(symbol, interval)] = s;}
 public IndicatorSnapshotMessage? Process(MarketCandle c, DateTimeOffset now){if(!c.IsClosed || !_states.TryGetValue(Key(c.Symbol, c.Interval), out var s))return null;var v = s.Add(c);if(v is null)return null;return new(){Type = Name, Symbol = c.Symbol, Timeframe = c.Interval, CandleOpenTime = new DateTimeOffset(c.OpenTimeUtc).ToUnixTimeMilliseconds(), CandleCloseTime = new DateTimeOffset(c.CloseTimeUtc).ToUnixTimeMilliseconds(), PublishedAt = now.ToUnixTimeMilliseconds(), Indicators = new Dictionary<string, IndicatorValueMessage>{{"alligator_jaw", new(){Value = v.Value.jaw}}, {"alligator_teeth", new(){Value = v.Value.teeth}}, {"alligator_lips", new(){Value = v.Value.lips}}, {"sma200", new(){Value = v.Value.sma}}}};}
 private static string Key(string s, string i) => $"{s.ToUpperInvariant()}:{i.ToLowerInvariant()}";
 private sealed class State{readonly AlligatorOptions o;readonly Queue<MarketCandle> q = new();readonly SmoothedMovingAverage jaw, teeth, lips;DateTime last;
 public State(AlligatorOptions o){this.o = o;jaw = new(o.JawLength);teeth = new(o.TeethLength);lips = new(o.LipsLength);} public (decimal jaw, decimal teeth, decimal lips, decimal sma)? Add(MarketCandle c){if(c.CloseTimeUtc<=last)return null;last = c.CloseTimeUtc;q.Enqueue(c);while(q.Count>o.HistoryLimit)q.Dequeue();var h = (c.High+c.Low)/2;var j = jaw.Update(h);var t = teeth.Update(h);var l = lips.Update(h);if(q.Count<o.SmaLength)return null;return(j, t, l, q.TakeLast(o.SmaLength).Average(x => x.Close));}}
}
