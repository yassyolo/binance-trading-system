using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using System.Text;
using TradingSystem.Operations.Contracts;
using TradingSystem.Operations.Models;
using Xunit;

namespace TradingSystem.Dashboard.Api.Tests.Middlewares;

public sealed class AuditMiddlewareTests
{
    [Fact]
    public async Task NonGetRequest_ShouldRedactSensitiveJsonFields()
    {
        AuditEvent? captured = null;

        var audit = new Mock<IAuditLog>();

        audit
            .Setup(x => x.WriteAsync(
                It.IsAny<AuditEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>(
                (item, _) => captured = item)
            .Returns(Task.CompletedTask);

        var context = new DefaultHttpContext();

        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.Name,
                        "operator-a")
                },
                "Tests"));

        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/test";

        var body =
            """
            {
              "reason": "test",
              "apiKey": "TOP-SECRET",
              "nested": {
                "token": "TOKEN-SECRET"
              }
            }
            """;

        context.Request.Body = new MemoryStream(
            Encoding.UTF8.GetBytes(body));

        context.Response.Body = new MemoryStream();

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode =
                StatusCodes.Status202Accepted;

            return Task.CompletedTask;
        };

        var middleware =
            new global::AuditMiddleware(next);

        await middleware.InvokeAsync(
            context,
            audit.Object,
            NullLogger<global::AuditMiddleware>.Instance);

        Assert.NotNull(captured);
        Assert.Equal("operator-a", captured!.Actor);
        Assert.Equal(
            "POST /api/v1/test",
            captured.Action);

        Assert.Contains(
            "[REDACTED]",
            captured.NewValueJson);

        Assert.DoesNotContain(
            "TOP-SECRET",
            captured.NewValueJson);

        Assert.DoesNotContain(
            "TOKEN-SECRET",
            captured.NewValueJson);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task ReadOnlyMethods_ShouldNotWriteAudit(
        string method)
    {
        var audit = new Mock<IAuditLog>();
        var context = new DefaultHttpContext();

        context.Request.Method = method;

        var middleware =
            new global::AuditMiddleware(
                _ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context,
            audit.Object,
            NullLogger<global::AuditMiddleware>.Instance);

        audit.Verify(
            x => x.WriteAsync(
                It.IsAny<AuditEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
