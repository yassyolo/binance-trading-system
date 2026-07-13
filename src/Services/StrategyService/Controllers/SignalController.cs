using Microsoft.AspNetCore.Mvc;
using StrategyService.Signals;
using TradingSystem.Application.Execution;
using TradingSystem.Domain.Signals;

namespace StrategyService.Controllers;

[ApiController]
[Route("api/signals")]
public sealed class SignalController(ITradingSignalHandler signalHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ReceiveAsync(
        [FromBody] TradeSignal signal,
        CancellationToken cancellationToken)
    {
        var processed = await signalHandler.HandleAsync(
            signal,
            cancellationToken);

        return Ok(new
        {
            status = processed ? "processed" : "ignored",
            signal.Symbol,
            signal.Source,
            ts = DateTime.UtcNow
        });
    }
}