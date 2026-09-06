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
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(interval);
        ArgumentNullException.ThrowIfNull(candles);

        var state = new AlligatorState(_options); 
		
		foreach (var candle in candles.Where(x => x.IsClosed).OrderBy(x => x.CloseTimeUtc).TakeLast(_options.HistoryLimit)) 
			state.Add(candle); 
		
		_states[Key(symbol, interval)] = state; 
	}
	
	public IndicatorSnapshotMessage? Process(MarketCandle candle, DateTimeOffset now) 
	{ 
		if (!candle.IsClosed || !_states.TryGetValue(Key(candle.Symbol, candle.Interval), out var state)) 
			return null; 
		
		var values = state.Add(candle); 	
		if (values is null) 
			return null; 
		
		return new() 
		{ 
			Type = Name, 
			Symbol = candle.Symbol, 
			Timeframe = candle.Interval,
			CandleOpenTime = new DateTimeOffset(candle.OpenTimeUtc).ToUnixTimeMilliseconds(), 
			CandleCloseTime = new DateTimeOffset(candle.CloseTimeUtc).ToUnixTimeMilliseconds(), 
			PublishedAt = now.ToUnixTimeMilliseconds(),
			Indicators = new Dictionary<string, IndicatorValueMessage> 
			{ 
				{ "alligator_jaw", new() { Value = values.Value.jaw } }, 
				{ "alligator_teeth", new() { Value = values.Value.teeth } }, 
				{ "alligator_lips", new() { Value = values.Value.lips } },
				{ "sma200", new() { Value = values.Value.sma } } 
			} 
		}; 
	}
	
	private static string Key(string s, string i) 
		=> $"{s.ToUpperInvariant()}:{i.ToLowerInvariant()}";
}
