using TradingSystem.Dashboard.Api;
using TradingSystem.Dashboard.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDashboardApi(
    builder.Configuration,
    builder.Environment);

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 1_048_576);

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/swagger"),
        branch =>
        {
            branch.UseMiddleware<SecurityHeadersMiddleware>();
        });
}
else
{
    app.UseMiddleware<SecurityHeadersMiddleware>();
}

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors(DependencyInjection.ReactCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
        options.DisplayRequestDuration());
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<AuditMiddleware>();

if (!app.Environment.IsDevelopment() &&
    builder.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/swagger"),
        branch =>
        {
            branch.Use(async (context, next) =>
            {
                if (context.User.Identity?.IsAuthenticated != true ||
                    !context.User.IsInRole("Administrator"))
                {
                    context.Response.StatusCode =
                        StatusCodes.Status404NotFound;

                    return;
                }

                await next(context);
            });
        });

    app.UseSwagger();

    app.UseSwaggerUI(options =>
        options.DisplayRequestDuration());
}

app.MapControllers();

app.MapHealthChecks("/health/live")
    .AllowAnonymous()
    .DisableRateLimiting();

app.Run();

public partial class Program;