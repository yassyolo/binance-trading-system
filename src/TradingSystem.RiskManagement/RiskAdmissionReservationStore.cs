using System.Collections.Concurrent;
using TradingSystem.Domain.Enums;

namespace TradingSystem.RiskManagement;

public sealed record RiskAdmissionReservation(
    Guid ReservationId,
    string SignalId,
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

    bool RemoveBySignalId(string signalId);
}

public sealed class InMemoryRiskAdmissionReservationStore
    : IRiskAdmissionReservationStore
{
    private readonly ConcurrentDictionary<Guid, RiskAdmissionReservation>
        _reservations = new();

    public IReadOnlyCollection<RiskAdmissionReservation> GetActive(
        DateTime nowUtc)
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
        ArgumentNullException.ThrowIfNull(reservation);

        if (string.IsNullOrWhiteSpace(reservation.SignalId))
            throw new ArgumentException(
                "Risk reservation SignalId is required.",
                nameof(reservation));

        // A retried evaluation of the same signal must not create two active
        // reservations. Remove the old reservation before storing the new one.
        RemoveBySignalId(reservation.SignalId);
        _reservations[reservation.ReservationId] = reservation;
    }

    public bool RemoveBySignalId(string signalId)
    {
        if (string.IsNullOrWhiteSpace(signalId))
            return false;

        var removed = false;

        foreach (var item in _reservations)
        {
            if (!item.Value.SignalId.Equals(
                    signalId,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            removed |= _reservations.TryRemove(item.Key, out _);
        }

        return removed;
    }
}
