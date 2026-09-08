using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/charts")]
public sealed class ChartsController(
    IDashboardQueryStore queryStore)
    : DashboardControllerBase
{
    [HttpGet("{symbol}/{interval}")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(string symbol, string interval, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        RequestValidation.ValidateChart(symbol, interval, fromUtc, toUtc);

        var result = await queryStore.GetPriceChartAsync(symbol, interval, fromUtc, toUtc, ct);

        return Ok(result);
    }
}
