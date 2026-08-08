namespace TradingSystem.Dashboard.Contracts.Models.Charts;

public sealed record PriceChartDto(IReadOnlyCollection<PriceCandleDto> Candles, IReadOnlyCollection<ChartMarkerDto> Markers);

