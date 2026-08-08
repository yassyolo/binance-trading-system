using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace TradingSystem.Dashboard.Api.Tests.Infrastructure;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    public const string SchemeName = "DashboardTests";
    public const string RoleHeader = "X-Test-Role";
    public const string UserHeader = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers[RoleHeader].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var user = Request.Headers[UserHeader].FirstOrDefault()
            ?? "integration-test-user";

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user),
            new Claim("sub", user),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(
            claims,
            SchemeName,
            ClaimTypes.Name,
            ClaimTypes.Role);

        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(
                    principal,
                    SchemeName)));
    }
}
