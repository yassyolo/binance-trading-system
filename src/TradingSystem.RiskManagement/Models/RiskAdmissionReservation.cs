using TradingSystem.Domain.Enums;

namespace TradingSystem.RiskManagement.Models;

public sealed record RiskAdmissionReservation(
    Guid ReservationId,
    string SignalId,
    string BotName,
    string Symbol,
    PositionSide Side,
    decimal Quantity,
    decimal Notional,
    DateTime ExpiresAtUtc);
