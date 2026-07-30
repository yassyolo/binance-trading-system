using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingSystem.PortfolioManagement;

public sealed class NullPaperPortfolioPositionSource : IPaperPortfolioPositionSource
{
    public Task<IReadOnlyCollection<PaperPortfolioPosition>> GetOpenAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<PaperPortfolioPosition>>(
            Array.Empty<PaperPortfolioPosition>());
    }
}

