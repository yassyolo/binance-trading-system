using System.Diagnostics;

namespace TradingSystem.Dashboard.Api.Middlewares;

public sealed class RequestLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context,  ILogger<RequestLoggingMiddleware> logger)
    {
        var started  =  Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsed  =  Stopwatch.GetElapsedTime(started);
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F1} ms for {User}", 
                context.Request.Method,  context.Request.Path,  context.Response.StatusCode,  elapsed.TotalMilliseconds, 
                context.User.Identity?.Name ?? "anonymous");
        }
    }
}
