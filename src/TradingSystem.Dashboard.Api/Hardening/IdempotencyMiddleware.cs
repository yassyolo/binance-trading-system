using System.Security.Cryptography;
using System.Text;
using Dapper;
using TradingSystem.Persistence.PostgreSql.Connections;

namespace TradingSystem.Dashboard.Api.Hardening;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Idempotency-Key";
    private const int MaximumStoredResponseBytes = 1_048_576;

    public async Task InvokeAsync(
        HttpContext context,
        ITradingDbConnectionFactory connections,
        ILogger<IdempotencyMiddleware> logger)
    {
        if (!RequiresIdempotency(context.Request))
        {
            await next(context);
            return;
        }

        var key = context.Request.Headers[HeaderName].FirstOrDefault();
        if (!IsValidKey(key))
            throw new ApiValidationException(new Dictionary<string, string[]>
            {
                [HeaderName] = ["A 16-128 character idempotency key is required for this operation."]
            });

        context.Request.EnableBuffering();
        var requestBody = await ReadBodyAsync(context.Request, context.RequestAborted);
        context.Request.Body.Position = 0;

        var requestIdentity = string.Join('|',
            context.Request.Method,
            context.Request.Path.Value,
            context.Request.QueryString.Value,
            context.Request.ContentType,
            requestBody);
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestIdentity)));
        var actor = context.User.Identity?.Name ?? context.User.FindFirst("sub")?.Value ?? "anonymous";

        await using var connection = await connections.OpenAsync(context.RequestAborted);
        await RemoveExpiredAsync(connection, key!, actor, context.RequestAborted);

        var inserted = await connection.ExecuteAsync(new CommandDefinition(@"
            insert into trading_dashboard.api_idempotency_keys
                (idempotency_key, actor, method, path, request_hash, status, created_at_utc, expires_at_utc)
            values (@key, @actor, @method, @path, @requestHash, 'Processing', now(), now() + interval '24 hours')
            on conflict (idempotency_key, actor) do nothing;",
            new
            {
                key,
                actor,
                method = context.Request.Method,
                path = context.Request.Path.Value,
                requestHash
            },
            cancellationToken: context.RequestAborted));

        if (inserted == 0)
        {
            var existing = await connection.QuerySingleAsync<ExistingRequest>(new CommandDefinition(@"
                select request_hash RequestHash, status Status, response_status_code ResponseStatusCode,
                       response_content_type ResponseContentType, response_body ResponseBody
                from trading_dashboard.api_idempotency_keys
                where idempotency_key = @key and actor = @actor;",
                new { key, actor },
                cancellationToken: context.RequestAborted));

            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                throw new ApiConflictException(
                    "The idempotency key was already used with a different request.");

            if (existing.Status == "Completed" && existing.ResponseStatusCode.HasValue)
            {
                context.Response.StatusCode = existing.ResponseStatusCode.Value;
                if (!string.IsNullOrWhiteSpace(existing.ResponseContentType))
                    context.Response.ContentType = existing.ResponseContentType;
                context.Response.Headers["X-Idempotent-Replay"] = "true";
                if (existing.ResponseBody is { Length: > 0 })
                    await context.Response.Body.WriteAsync(existing.ResponseBody, context.RequestAborted);
                return;
            }

            throw new ApiConflictException("An identical request is already being processed.");
        }

        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await next(context);

            // Replaying an empty body for a large successful response is incorrect. Large
            // responses are delivered normally, but the key is released so a later retry
            // executes the operation instead of receiving a truncated replay.
            if (responseBuffer.Length <= MaximumStoredResponseBytes)
            {
                var responseBytes = responseBuffer.ToArray();
                await connection.ExecuteAsync(new CommandDefinition(@"
                    update trading_dashboard.api_idempotency_keys
                    set status = 'Completed', response_status_code = @statusCode,
                        response_content_type = @contentType, response_body = @responseBody,
                        completed_at_utc = now()
                    where idempotency_key = @key and actor = @actor and status = 'Processing';",
                    new
                    {
                        key,
                        actor,
                        statusCode = context.Response.StatusCode,
                        contentType = context.Response.ContentType,
                        responseBody = responseBytes
                    },
                    cancellationToken: context.RequestAborted));
            }
            else
            {
                await ReleaseAsync(connection, key!, actor, CancellationToken.None);
                logger.LogWarning(
                    "Response for idempotency key {IdempotencyKey} was {Length} bytes and was not cached.",
                    key,
                    responseBuffer.Length);
            }

            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch
        {
            try
            {
                await ReleaseAsync(connection, key!, actor, CancellationToken.None);
            }
            catch (Exception cleanupError)
            {
                logger.LogWarning(cleanupError,
                    "Could not release idempotency key {IdempotencyKey}.", key);
            }
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool RequiresIdempotency(HttpRequest request) =>
        request.Method == HttpMethods.Post &&
        (request.Path.StartsWithSegments("/api/v1/bots") ||
         request.Path.StartsWithSegments("/api/v1/backtests") ||
         request.Path.StartsWithSegments("/api/v1/optimizations") ||
         request.Path.StartsWithSegments("/api/v1/replays") ||
         request.Path.StartsWithSegments("/api/v1/paper"));

    private static bool IsValidKey(string? key) =>
        key is { Length: >= 16 and <= 128 } &&
        key.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');

    private static async Task<string> ReadBodyAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, false, 4096, true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static Task RemoveExpiredAsync(
        System.Data.Common.DbConnection connection,
        string key,
        string actor,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(@"
            delete from trading_dashboard.api_idempotency_keys
            where idempotency_key = @key and actor = @actor and expires_at_utc <= now();",
            new { key, actor }, cancellationToken: cancellationToken));

    private static Task ReleaseAsync(
        System.Data.Common.DbConnection connection,
        string key,
        string actor,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(@"
            delete from trading_dashboard.api_idempotency_keys
            where idempotency_key = @key and actor = @actor and status = 'Processing';",
            new { key, actor }, cancellationToken: cancellationToken));

    private sealed record ExistingRequest(
        string RequestHash,
        string Status,
        int? ResponseStatusCode,
        string? ResponseContentType,
        byte[]? ResponseBody);
}
