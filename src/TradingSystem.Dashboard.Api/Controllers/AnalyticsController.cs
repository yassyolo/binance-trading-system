using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class AnalyticsController(
    IDashboardQueryStore queryStore)
    : DashboardControllerBase
{
    [HttpGet("analytics")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAnalyticsAsync(string? botName, string? symbol, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var result = await queryStore.GetAnalyticsAsync(new(BotName: botName, Symbol: symbol, FromUtc: fromUtc, ToUtc: toUtc), ct);

        return Ok(result);
    }

    [HttpGet("analytics/equity")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetEquityAsync(string? botName, CancellationToken ct)
    {
        var result = await queryStore.GetEquityAsync(new(BotName: botName), ct);

        return Ok(result);
    }

    [HttpGet("comparisons")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> CompareRunsAsync(Guid leftRunId, Guid rightRunId, CancellationToken ct)
    {
        var comparison = await queryStore.CompareRunsAsync(leftRunId, rightRunId, ct);

        return comparison is null ? NotFound() : Ok(comparison);
    }
}
