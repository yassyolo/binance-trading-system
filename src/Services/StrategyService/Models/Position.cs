namespace StrategyService.Models;

public sealed class Position
{
    public string PositionId { get; set; } = Guid.NewGuid().ToString("N");

    public string Symbol { get; set; } = string.Empty;

    public string Side { get; set; } = string.Empty;

    public decimal EntryPrice { get; set; }

    public decimal Quantity { get; set; }

    public decimal RemainingQuantity { get; set; }

    public decimal TakeProfitPrice { get; set; }

    public decimal StopLossPrice { get; set; }

    public decimal Stop3Price { get; set; }

    public bool TakeProfitExecuted { get; set; }

    public bool StopLossExecuted { get; set; }

    public bool Stop3Active { get; set; }

    public bool Closed { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}