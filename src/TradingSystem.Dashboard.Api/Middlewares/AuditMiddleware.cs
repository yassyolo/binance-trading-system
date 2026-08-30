using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TradingSystem.Operations.Contracts;

public sealed class AuditMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "token", "apiKey", "apiSecret", "signingKey", "authorization", "connectionString"
    };

    public async Task InvokeAsync(HttpContext context, IAuditLog audit, ILogger<AuditMiddleware> logger)
    {
        if (context.Request.Method is "GET" or "HEAD" or "OPTIONS")
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        string? body;
        using (var reader  =  new StreamReader(context.Request.Body,  Encoding.UTF8,  false,  4096,  true))
        {
            body  =  await reader.ReadToEndAsync(context.RequestAborted);
            context.Request.Body.Position  =  0;
        }

        var succeeded  =  false;
        try
        {
            await next(context);
            succeeded = true;
        }
        finally
        {
            var actor = context.User.Identity?.Name ?? context.User.FindFirst("sub")?.Value ?? "anonymous";
            var action = $"{context.Request.Method} {context.Request.Path}";
            var metadata = new Dictionary<string,  string>
            {
                ["statusCode"] = context.Response.StatusCode.ToString(), 
                ["succeeded"] = succeeded.ToString(), 
                ["userAgent"] = Truncate(context.Request.Headers.UserAgent.ToString(),  256)
            };

            try
            {
                await audit.WriteAsync(new(
                    Guid.NewGuid(),  
                    DateTime.UtcNow,  
                    actor, 
                    action, 
                    "HttpRequest", 
                    context.Request.RouteValues.Values.LastOrDefault()?.ToString(), 
                    context.Request.Headers["X-Change-Reason"].FirstOrDefault(), 
                    context.TraceIdentifier, 
                    context.Connection.RemoteIpAddress?.ToString(), 
                    null, 
                    Redact(body), 
                    metadata),  
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,  "Audit event could not be persisted for {Action}. TraceId: {TraceId}",  action,  context.TraceIdentifier);
            }
        }
    }

    private static string? Redact(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        
        if (body.Length > 64_000) 
            return JsonSerializer.Serialize(new { truncated  =  true,  originalLength  =  body.Length });

        try
        {
            var node  =  JsonNode.Parse(body);
            RedactNode(node);
            return node?.ToJsonString();
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { nonJsonPayload  =  true,  length  =  body.Length });
        }
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToArray())
            {
                if (SensitiveNames.Contains(property.Key)) obj[property.Key]  =  "[REDACTED]";
                else RedactNode(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array) RedactNode(item);
        }
    }

    private static string Truncate(string value,  int maximum)  =>  value.Length <= maximum ? value : value[..maximum];
}
