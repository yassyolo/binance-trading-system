using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using TradingSystem.Dashboard.Api.Middlewares;

namespace TradingSystem.Dashboard.Api.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) 
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var descriptor = Describe(ctx, ex);

        if (descriptor.Status >= 500)
            logger.LogError(ex, "Unhandled API ex. TraceId: {TraceId}", ctx.TraceIdentifier);
        else
            logger.LogWarning(ex, "API request failed with {StatusCode}. TraceId: {TraceId}", descriptor.Status, ctx.TraceIdentifier);

        ctx.Response.StatusCode = descriptor.Status;
        var problem = new ProblemDetails
        {
            Status = descriptor.Status,
            Title = descriptor.Title,
            Type = descriptor.Type,
            Detail = descriptor.ExposeMessage ? ex.Message : descriptor.PublicDetail,
            Instance = ctx.Request.Path
        };
        problem.Extensions["traceId"] = ctx.TraceIdentifier;
        problem.Extensions["correlationId"] = ctx.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
       
        if (descriptor.Errors is not null)
            problem.Extensions["errors"] = descriptor.Errors;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = problem
        });
    }

    private static ErrorDescriptor Describe(HttpContext context, Exception exception) => exception switch
    {
        ApiValidationException validation => new(StatusCodes.Status400BadRequest, "Validation failed",
            "https://trading-system/errors/validation",
            true,
            validation.Errors),
        ApiConflictException => new(StatusCodes.Status409Conflict, "Conflict",
            "https://trading-system/errors/conflict",
            true),
        ApiNotFoundException => new(StatusCodes.Status404NotFound, "Resource not found",
            "https://trading-system/errors/not-found",
            true),
        UnauthorizedAccessException => new(StatusCodes.Status403Forbidden, "Forbidden",
            "https://trading-system/errors/forbidden",
            false,
            PublicDetail: "You are not allowed to perform this operation."),
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => new(StatusCodes.Status409Conflict, "Duplicate request",
            "https://trading-system/errors/duplicate",
            false,
            PublicDetail: "A conflicting resource already exists."),
        OperationCanceledException when context.RequestAborted.IsCancellationRequested => new(499, "Request cancelled",
            "https://trading-system/errors/cancelled",
            false,
            PublicDetail: "The client cancelled the request."),
        _ => new(StatusCodes.Status500InternalServerError, "Unexpected server error",
            "https://trading-system/errors/internal",
            false,
            PublicDetail: "An unexpected error occurred.")
    };

    private sealed record ErrorDescriptor(
        int Status,
        string Title,
        string Type,
        bool ExposeMessage,
        IReadOnlyDictionary<string, string[]>? Errors = null,
        string? PublicDetail = null);
}
