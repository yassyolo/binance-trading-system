using TradingSystem.RiskManagement.Models;

namespace TradingSystem.RiskManagement.Contracts;

public interface IRiskAdmissionReservationStore
{
    IReadOnlyCollection<RiskAdmissionReservation> GetActive(DateTime nowUtc);

    void Add(RiskAdmissionReservation reservation);

    bool RemoveBySignalId(string signalId);
}
