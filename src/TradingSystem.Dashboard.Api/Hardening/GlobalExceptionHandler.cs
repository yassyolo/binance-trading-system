using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace TradingSystem.Dashboard.Api.Hardening;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails, 
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context,  Exception exception,  CancellationToken cancellationToken)
    {
        (int status,  string title,  string type,  IReadOnlyDictionary<string,  string[]>? errors)  =  exception switch
        {
            ApiValidationException validation  => 
                (StatusCodes.Status400BadRequest,  "Validation failed",  "https://trading-system/errors/validation",  validation.Errors), 
            ApiConflictException or InvalidOperationException  => 
                (StatusCodes.Status409Conflict,  "Conflict",  "https://trading-system/errors/conflict",  null), 
            ApiNotFoundException  => 
                (StatusCodes.Status404NotFound,  "Resource not found",  "https://trading-system/errors/not-found",  null), 
            UnauthorizedAccessException  => 
                (StatusCodes.Status403Forbidden,  "Forbidden",  "https://trading-system/errors/forbidden",  null), 
            PostgresException { SqlState: "23505" }  => 
                (StatusCodes.Status409Conflict,  "Duplicate request",  "https://trading-system/errors/duplicate",  null), 
            OperationCanceledException when context.RequestAborted.IsCancellationRequested  => 
                (499,  "Request cancelled",  "https://trading-system/errors/cancelled",  null), 
            _  => 
                (StatusCodes.Status500InternalServerError,  "Unexpected server error",  "https://trading-system/errors/internal",  null)
        };

        if (status >= 500)
            logger.LogError(exception,  "Unhandled API exception. TraceId: {TraceId}",  context.TraceIdentifier);
        else
            logger.LogWarning(exception,  "API request failed with {StatusCode}. TraceId: {TraceId}",  status,  context.TraceIdentifier);

        context.Response.StatusCode  =  status;
        var problem  =  new ProblemDetails
        {
            Status  =  status, 
            Title  =  title, 
            Type  =  type, 
            Detail  =  status >= 500 ? "An unexpected error occurred." : exception.Message, 
            Instance  =  context.Request.Path
        };
        problem.Extensions["traceId"]  =  context.TraceIdentifier;
        problem.Extensions["correlationId"]  =  context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        if (errors is not null)
            problem.Extensions["errors"]  =  errors;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext  =  context, 
            ProblemDetails  =  problem
        });
    }
}
