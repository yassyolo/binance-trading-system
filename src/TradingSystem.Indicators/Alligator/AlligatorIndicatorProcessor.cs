using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TradingSystem.Contracts.Indicators;
using TradingSystem.Domain.MarketData;
using TradingSystem.Indicators.Alligator.Configuration;
using TradingSystem.Indicators.Alligator.Models;
using TradingSystem.Indicators.Contracts;

namespace TradingSystem.Indicators.Alligator;

public sealed class AlligatorIndicatorProcessor(
	IOptions<AlligatorOptions> options) 
	: IIndicatorProcessor
{
	private readonly AlligatorOptions _options = options.Value; 
	private readonly ConcurrentDictionary<string, AlligatorState> _states = new();
	
	public string Name => "alligator_ma"; 
	
	public IReadOnlyCollection<string> Symbols => _options.Symbols; 
	
	public IReadOnlyCollection<string> Intervals => _options.Intervals; 
	
	public int RequiredHistory => Math.Max(_options.HistoryLimit, _options.SmaLength);
	
	public void Initialize(string symbol, string interval, IReadOnlyList<MarketCandle> candles) 
	{ 
		var s = new AlligatorState(_options); 
		
		foreach (var c in candles.Where(x => x.IsClosed).OrderBy(x => x.CloseTimeUtc).TakeLast(_options.HistoryLimit)) 
			s.Add(c); 
		
		_states[Key(symbol, interval)] = s; 
	}
	
	public IndicatorSnapshotMessage? Process(MarketCandle c, DateTimeOffset now) 
	{ 
		if (!c.IsClosed || !_states.TryGetValue(Key(c.Symbol, c.Interval), out var s)) 
			return null; 
		
		var v = s.Add(c); 
		
		if (v is null) 
			return null; 
		
		return new() 
		{ 
			Type = Name, 
			Symbol = c.Symbol, 
			Timeframe = c.Interval,
			CandleOpenTime = new DateTimeOffset(c.OpenTimeUtc).ToUnixTimeMilliseconds(), 
			CandleCloseTime = new DateTimeOffset(c.CloseTimeUtc).ToUnixTimeMilliseconds(), 
			PublishedAt = now.ToUnixTimeMilliseconds(),
			Indicators = new Dictionary<string, IndicatorValueMessage> 
			{ 
				{ "alligator_jaw", new() { Value = v.Value.jaw } }, 
				{ "alligator_teeth", new() { Value = v.Value.teeth } }, 
				{ "alligator_lips", new() { Value = v.Value.lips } },
				{ "sma200", new() { Value = v.Value.sma } } 
			} 
		}; 
	}
	
	private static string Key(string s, string i) 
		=> $"{s.ToUpperInvariant()}:{i.ToLowerInvariant()}";
}
