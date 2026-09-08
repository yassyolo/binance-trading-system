using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
public sealed class AuditController(
    IDashboardQueryStore queryStore)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(string? actor, string? action, DateTime? fromUtc, DateTime? toUtc, int skip, int take, CancellationToken ct)
    {
        var result = await queryStore.GetAuditEventsAsync(actor, action, fromUtc, toUtc, RequestValidation.Skip(skip), RequestValidation.PageSize(take), ct);

        return Ok(result);
    }
}
