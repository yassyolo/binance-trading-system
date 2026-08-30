using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class AnalyticsController(
    IDashboardQueryStore store)
    : DashboardControllerBase
{
    [HttpGet("analytics")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAnalyticsAsync(
        string? botName,
        string? symbol,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var result = await store.GetAnalyticsAsync(
            new(
                BotName: botName,
                Symbol: symbol,
                FromUtc: fromUtc,
                ToUtc: toUtc),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("analytics/equity")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetEquityAsync(
        string? botName,
        CancellationToken cancellationToken)
    {
        var result = await store.GetEquityAsync(
            new(BotName: botName),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("comparisons")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> CompareRunsAsync(Guid leftRunId, Guid rightRunId, CancellationToken cancellationToken)
    {
        var comparison = await store.CompareRunsAsync(leftRunId, rightRunId, cancellationToken);

        return comparison is null ? NotFound() : Ok(comparison);
    }
}
