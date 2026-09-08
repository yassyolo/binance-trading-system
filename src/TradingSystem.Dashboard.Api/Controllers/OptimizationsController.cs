using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Dashboard.Contracts.Models.Optimization;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/optimizations")]
public sealed class OptimizationsController(
    IDashboardJobStore jobStore,
    IDashboardQueryStore queryStore)
    : DashboardControllerBase
{
    [HttpPost]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateAsync(OptimizationRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);

        var result = await jobStore.EnqueueOptimizationAsync(request, DashboardUserName, ct);

        return Accepted(value: result);
    }

    [HttpGet("{runId:guid}/trials")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetTrialsAsync(Guid runId, int take, CancellationToken ct)
    {
        var result = await queryStore.GetOptimizationTrialsAsync(runId, RequestValidation.PageSize(take), ct);

        return Ok(result);
    }
}
