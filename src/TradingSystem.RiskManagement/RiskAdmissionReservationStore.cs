using System.Collections.Concurrent;
using TradingSystem.Domain.Enums;

namespace TradingSystem.RiskManagement;

public sealed record RiskAdmissionReservation(
    Guid ReservationId,
    string BotName,
    string Symbol,
    PositionSide Side,
    decimal Quantity,
    decimal Notional,
    DateTime ExpiresAtUtc);

public interface IRiskAdmissionReservationStore
{
    IReadOnlyCollection<RiskAdmissionReservation> GetActive(DateTime nowUtc);
    void Add(RiskAdmissionReservation reservation);
}

public sealed class InMemoryRiskAdmissionReservationStore : IRiskAdmissionReservationStore
{
    private readonly ConcurrentDictionary<Guid, RiskAdmissionReservation> _reservations = new();

    public IReadOnlyCollection<RiskAdmissionReservation> GetActive(DateTime nowUtc)
    {
        foreach (var item in _reservations)
        {
            if (item.Value.ExpiresAtUtc <= nowUtc)
                _reservations.TryRemove(item.Key, out _);
        }

        return _reservations.Values.ToArray();
    }

    public void Add(RiskAdmissionReservation reservation)
    {
        _reservations[reservation.ReservationId] = reservation;
    }
}
