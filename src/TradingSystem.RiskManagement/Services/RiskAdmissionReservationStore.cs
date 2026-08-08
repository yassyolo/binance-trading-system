using System.Collections.Concurrent;
using TradingSystem.RiskManagement.Contracts;
using TradingSystem.RiskManagement.Models;

namespace TradingSystem.RiskManagement.Services;

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
        ArgumentNullException.ThrowIfNull(reservation);

        if (string.IsNullOrWhiteSpace(reservation.SignalId))
            throw new ArgumentException("Risk reservation SignalId is required.", nameof(reservation));

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
            if (!item.Value.SignalId.Equals(signalId, StringComparison.OrdinalIgnoreCase))
                continue;

            removed |= _reservations.TryRemove(item.Key, out _);
        }

        return removed;
    }
}
