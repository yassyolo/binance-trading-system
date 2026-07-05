using Microsoft.AspNetCore.Mvc;
using StrategyService.Services;
using TradingSystem.Domain.Signals;

namespace StrategyService.Controllers;

[ApiController]
[Route("api/signals")]
public sealed class SignalController : ControllerBase
{
    private readonly SignalProcessor _signalProcessor;

    public SignalController(SignalProcessor signalProcessor)
    {
        _signalProcessor = signalProcessor;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync(
        [FromBody] TradingSignal signal,
        CancellationToken cancellationToken)
    {
        var processed = await _signalProcessor.ProcessAsync(
            signal,
            cancellationToken);

        return Ok(new
        {
            status = processed ? "processed" : "ignored",
            signal.Action,
            signal.Symbol,
            signal.Source,
            ts = DateTime.UtcNow
        });
    }
}