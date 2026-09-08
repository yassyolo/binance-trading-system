using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TradingSystem.PaperTrading.Configuration;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.PaperTrading.Models.Enums;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/paper")]
public sealed class PaperTradingController(
    IPaperTradingStore paperTradingStore,
    IOptions<PaperTradingOptions> options)
    : DashboardControllerBase
{
    [HttpGet("account")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAccountAsync(CancellationToken ct)
    {
        var result = await paperTradingStore.GetAccountAsync(options.Value.InitialBalance, ct);

        return Ok(result);
    }

    [HttpGet("positions")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetPositionsAsync(string? botName, string? symbol, PaperPositionStatus? status, int? skip, int? take, CancellationToken ct)
    {
        var result = await paperTradingStore.QueryAsync(botName, symbol, status, Math.Max(0, skip ?? 0), Math.Clamp(take ?? 100, 1, 500), ct);

        return Ok(result);
    }

    [HttpPost("reset")]
    [Authorize(Policy = "Administrator")]
    [EnableRateLimiting("dangerous")]
    public async Task<IActionResult> ResetAsync(CancellationToken ct)
    {
        await paperTradingStore.ResetAsync(DashboardUserName, ct);

        return Accepted();
    }
}
