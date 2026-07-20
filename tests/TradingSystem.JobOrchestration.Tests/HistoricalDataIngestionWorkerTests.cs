using TradingSystem.Domain.MarketData;using TradingSystem.Jobs.Worker.Workers;
namespace TradingSystem.JobOrchestration.Tests;
public sealed class HistoricalDataIngestionWorkerTests
{
 [Fact]public void ParseInterval_ShouldParseMinutesHoursAndDays(){Assert.Equal(TimeSpan.FromMinutes(5),HistoricalDataIngestionWorker.ParseInterval("5m"));Assert.Equal(TimeSpan.FromHours(1),HistoricalDataIngestionWorker.ParseInterval("1h"));Assert.Equal(TimeSpan.FromDays(1),HistoricalDataIngestionWorker.ParseInterval("1d"));}
 [Fact]public void DetectGaps_ShouldReturnMissingRange(){var candles=new[]{C(0),C(1),C(4)};var gaps=HistoricalDataIngestionWorker.DetectGaps("BTCUSDC","1m",candles,TimeSpan.FromMinutes(1));var gap=Assert.Single(gaps);Assert.Equal(2,gap.MissingCandles);Assert.Equal(candles[1].OpenTimeUtc.AddMinutes(1),gap.FromUtc);}
 private static MarketCandle C(int minute){var open=new DateTime(2026,1,1,0,minute,0,DateTimeKind.Utc);return new("BTCUSDC","1m",open,open.AddMinutes(1).AddMilliseconds(-1),1,1,1,1,1,true);}
}
