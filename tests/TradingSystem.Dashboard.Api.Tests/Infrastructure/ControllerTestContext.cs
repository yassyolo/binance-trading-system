using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TradingSystem.Dashboard.Api.Tests.Infrastructure;

public static class ControllerTestContext
{
    public static T WithUser<T>(
        this T controller,
        string role = "Administrator",
        string user = "controller-test-user")
        where T : ControllerBase
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, user),
                new Claim("sub", user),
                new Claim(ClaimTypes.Role, role)
            },
            "Tests");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

        return controller;
    }
}
