using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Dashboard.Contracts.Models.Backtesting;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/backtests")]
public sealed class BacktestsController(
    IDashboardJobStore jobStore)
    : DashboardControllerBase
{
    [HttpPost]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateAsync(BacktestRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);

        var result = await jobStore.EnqueueBacktestAsync(request, DashboardUserName, ct);

        return Accepted(value: result);
    }
}
