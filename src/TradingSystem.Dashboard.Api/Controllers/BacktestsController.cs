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
    IDashboardJobStore store)
    : DashboardControllerBase
{
    [HttpPost]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateAsync(
        BacktestRequest request,
        CancellationToken cancellationToken)
    {
        RequestValidation.Validate(request);

        var result = await store.EnqueueBacktestAsync(
            request,
            DashboardUserName,
            cancellationToken);

        return Accepted(value: result);
    }
}
