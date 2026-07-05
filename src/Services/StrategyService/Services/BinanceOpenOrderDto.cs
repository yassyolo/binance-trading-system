namespace StrategyService.Services;

public sealed class BinanceOpenOrderDto
{
    public required string ClientOrderId { get; init; }
    public required string Type { get; init; }
    public required string PositionSide { get; init; }
    public decimal Price { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public DateTime? UpdateTimeUtc { get; init; }
}