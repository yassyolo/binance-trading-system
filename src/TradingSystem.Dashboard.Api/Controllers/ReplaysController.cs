using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.ReplayEngine.Models;
using TradingSystem.ReplayEngine.Models.Enums;
using TradingSystem.ReplayEngine.Store;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/replays")]
public sealed class ReplaysController(
    IReplayJobStore store)
    : DashboardControllerBase
{
    [HttpPost]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateAsync(
        CreateReplayRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidation.Validate(request);

        var id = await store.EnqueueAsync(
            request with
            {
                BatchSize = Math.Clamp(
                    request.BatchSize,
                    1,
                    1000)
            },
            DashboardUserName,
            cancellationToken);

        return Accepted(
            $"/api/v1/replays/{id}",
            new { replayId = id });
    }

    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> QueryAsync(
        ReplayJobStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var result = await store.QueryAsync(
            status,
            RequestValidation.Skip(skip),
            RequestValidation.PageSize(take),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{replayId:guid}")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(
        Guid replayId,
        CancellationToken cancellationToken)
    {
        var job = await store.GetAsync(
            replayId,
            cancellationToken);

        return job is null
            ? NotFound()
            : Ok(job);
    }

    [HttpGet("{replayId:guid}/result")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetResultAsync(
        Guid replayId,
        CancellationToken cancellationToken)
    {
        var summary = await store.GetSummaryAsync(
            replayId,
            cancellationToken);

        return summary is null
            ? NotFound()
            : Ok(summary);
    }

    [HttpGet("{replayId:guid}/steps")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetStepsAsync(
        Guid replayId,
        long afterGlobalPosition,
        int take,
        CancellationToken cancellationToken)
    {
        var result = await store.GetStepsAsync(
            replayId,
            Math.Max(0, afterGlobalPosition),
            Math.Clamp(take, 1, 500),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{replayId:guid}/cancel")]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CancelAsync(Guid replayId,CancellationToken ct)
    {
        await store.CancelAsync(replayId, DashboardUserName, ct);

        return Accepted();
    }
}
