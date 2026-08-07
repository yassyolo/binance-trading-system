namespace TradingSystem.Dashboard.Api.Middlewares;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(()  => 
        {
            var headers  =  context.Response.Headers;
            headers["X-Content-Type-Options"]  =  "nosniff";
            headers["X-API-Version"]  =  "1";
            headers["X-Frame-Options"]  =  "DENY";
            headers["Referrer-Policy"]  =  "no-referrer";
            headers["Permissions-Policy"]  =  "camera = (),  microphone = (),  geolocation = ()";
            headers["Content-Security-Policy"]  =  "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            headers.Remove("Server");
            return Task.CompletedTask;
        });

        await next(context);
    }
}
