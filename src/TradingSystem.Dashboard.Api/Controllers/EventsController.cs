using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TradingSystem.Dashboard.Api.Validation;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.Models;
using TradingSystem.EventStore.TradingTimeline;

namespace TradingSystem.Dashboard.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class EventsController(
    ITradingEventStoreReader eventStoreReader,
    ITradingEventStore eventStore)
    : DashboardControllerBase
{
    [HttpGet("eventStoreReader")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetTimelineAsync(
        string? aggregateType,
        string? aggregateId,
        string? botName,
        string? symbol,
        string? positionId,
        string? signalId,
        string? correlationId,
        string? eventType,
        DateTime? fromUtc,
        DateTime? toUtc,
        long? afterGlobalPosition,
        int skip,
        int take,
        CancellationToken ct)
    {
        var result = await eventStoreReader.ReadAsync(
            new EventStoreQuery(
                aggregateType,
                aggregateId,
                botName,
                symbol,
                positionId,
                signalId,
                correlationId,
                eventType,
                fromUtc,
                toUtc,
                afterGlobalPosition,
                RequestValidation.Skip(skip),
                RequestValidation.PageSize(take)),
            ct);

        return Ok(result);
    }

    [HttpGet("event-streams/{aggregateType}/{aggregateId}")]
    [Authorize(Policy = "Viewer")]
    [EnableRateLimiting("read")]
    public async Task<IActionResult> GetEventStreamAsync(string aggregateType, string aggregateId, long afterVersion, int take, CancellationToken ct)
    {
        var result = await eventStore.ReadStreamAsync(aggregateType, aggregateId, Math.Max(0, afterVersion), Math.Clamp(take, 1, 500), ct);

        return Ok(result);
    }
}
