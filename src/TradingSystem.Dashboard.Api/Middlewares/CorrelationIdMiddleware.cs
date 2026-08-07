using System.Diagnostics;

namespace TradingSystem.Dashboard.Api.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName  =  "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context,  ILogger<CorrelationIdMiddleware> logger)
    {
        var supplied  =  context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId  =  IsSafe(supplied) ? supplied! : Guid.NewGuid().ToString("N");

        context.TraceIdentifier  =  correlationId;
        context.Response.Headers[HeaderName]  =  correlationId;
        Activity.Current?.SetTag("correlation.id",  correlationId);

        using (logger.BeginScope(new Dictionary<string,  object> { ["CorrelationId"]  =  correlationId }))
            await next(context);
    }

    private static bool IsSafe(string? value)  => 
        !string.IsNullOrWhiteSpace(value)  &&  value.Length <= 128  &&  value.All(c  =>  char.IsLetterOrDigit(c)  ||  c is '-' or '_' or '.');
}
