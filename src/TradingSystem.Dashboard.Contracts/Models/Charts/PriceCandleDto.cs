namespace TradingSystem.Dashboard.Contracts.Models.Charts;

public sealed record PriceCandleDto(DateTime OpenTimeUtc, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);

