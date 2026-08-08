using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Exceptions;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.Dashboard.Application.Contracts;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
public sealed class AlertsController(
    IDashboardQueryStore queryStore,
    IAlertCommandStore commandStore)
    : DashboardControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetAsync(
        bool acknowledged,
        int take,
        CancellationToken cancellationToken)
    {
        var result = await queryStore.GetAlertsAsync(
            acknowledged,
            RequestValidation.PageSize(take),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:long}/acknowledge")]
    [Authorize(Policy = "Operator")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> AcknowledgeAsync(
        long id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ApiValidationException(
                new Dictionary<string, string[]>
                {
                    ["id"] =
                        ["Alert id must be positive."]
                });
        }

        await commandStore.AcknowledgeAsync(
            id,
            DashboardUserName,
            cancellationToken);

        return NoContent();
    }
}
