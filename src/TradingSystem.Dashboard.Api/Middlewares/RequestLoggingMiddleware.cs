using System.Diagnostics;

namespace TradingSystem.Dashboard.Api.Middlewares;

public sealed class RequestLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx,  ILogger<RequestLoggingMiddleware> logger)
    {
        var started = Stopwatch.GetTimestamp();
       
        try
        {
            await next(ctx);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started);
            
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F1} ms for {User}", ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, elapsed.TotalMilliseconds, ctx.User.Identity?.Name ?? "anonymous");
        }
    }
}
