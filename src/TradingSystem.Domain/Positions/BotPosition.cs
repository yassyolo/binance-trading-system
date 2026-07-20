using TradingSystem.Domain.Enums;
namespace TradingSystem.Domain.Positions;
public sealed class BotPosition
{
    public required string ShortId { get; init; }
    public required string BotName { get; init; }
    public required string Symbol { get; init; }
    public required PositionSide Side { get; init; }
    public required PositionMode Mode { get; init; }
    public decimal Quantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal? EntryPrice { get; set; }
    public string? ParentClientId { get; set; }
    public string? ParentOrderId { get; set; }
    public string? TpClientId { get; set; }
    public string? TpOrderId { get; set; }
    public decimal? TpPrice { get; set; }
    public string? TpStatus { get; set; }
    public bool TpExecuted { get; set; }
    public string? SlClientId { get; set; }
    public string? SlOrderId { get; set; }
    public decimal? SlPrice { get; set; }
    public string? SlStatus { get; set; }
    public bool SlExecuted { get; set; }
    public string? Stop3ClientId { get; set; }
    public string? Stop3OrderId { get; set; }
    public decimal? Stop3Current { get; set; }
    public decimal? Stop3Initial { get; set; }
    public decimal? Stop3Previous { get; set; }
    public decimal? Stop3NewPending { get; set; }
    public string? Stop3Status { get; set; }
    public bool Stop3Created { get; set; }
    public bool Stop3Pending { get; set; }
    public int TrailCount { get; set; }
    public bool TrailingInProgress { get; set; }
    public string? CloseClientId { get; set; }
    public string? CloseOrderId { get; set; }
    public string? CloseStatus { get; set; }
    public bool ProtectiveActive { get; set; }
    public bool ManualPosition { get; set; }
    public bool Closed { get; set; }
    public PositionStatus Status { get; set; } = PositionStatus.New;
    public string? Source { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ParentFilledAtUtc { get; set; }
    public DateTime? TpFilledAtUtc { get; set; }
    public DateTime? SlTriggeredAtUtc { get; set; }
    public DateTime? Stop3TriggeredAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public decimal? SignalCandleHigh { get; set; }
    public decimal? SignalCandleLow { get; set; }
    public long? SignalCandleCloseTime { get; set; }
    public bool HighReached { get; set; }

    public void MarkParentFilled(decimal entryPrice, string orderId, DateTime occurredAtUtc)
    { EntryPrice = entryPrice; ParentOrderId = orderId; ParentFilledAtUtc = occurredAtUtc; Status = PositionStatus.Open; UpdatedAtUtc = occurredAtUtc; }
    public void MarkTpFilled(decimal executedQuantity, DateTime occurredAtUtc)
    { RemainingQuantity = Math.Max(RemainingQuantity - executedQuantity, 0); TpExecuted = true; TpStatus = "FILLED"; TpFilledAtUtc = occurredAtUtc; UpdatedAtUtc = occurredAtUtc; if (RemainingQuantity == 0) MarkClosed("TAKE_PROFIT_FILLED", occurredAtUtc); else Status = PositionStatus.TpFilled; }
    public void MarkProtectiveOrderTerminal(string status, DateTime occurredAtUtc)
    { TpStatus = status; ProtectiveActive = false; UpdatedAtUtc = occurredAtUtc; }
    public void MarkClosing(DateTime occurredAtUtc) { Status = PositionStatus.Closing; UpdatedAtUtc = occurredAtUtc; }


    public void MarkClosed(string reason, DateTime occurredAtUtc)
    { Closed = true; RemainingQuantity = 0; ProtectiveActive = false; CloseStatus = reason; Status = PositionStatus.Closed; ClosedAtUtc = occurredAtUtc; UpdatedAtUtc = occurredAtUtc; }
}
