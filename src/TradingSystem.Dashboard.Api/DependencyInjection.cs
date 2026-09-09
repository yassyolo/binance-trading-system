using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using TradingSystem.Dashboard.Api.Authentication;
using TradingSystem.Dashboard.Api.Authentication.Models;
using TradingSystem.Dashboard.Api.Exceptions;
using TradingSystem.Dashboard.Api.Middlewares;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Operations;
using TradingSystem.PaperTrading.Configuration;
using TradingSystem.Persistence.PostgreSql;
using TradingSystem.Persistence.PostgreSql.Dashboard;

namespace TradingSystem.Dashboard.Api;

public static class DependencyInjection
{
    public const string ReactCorsPolicy = "React";

    public static IServiceCollection AddDashboardApi(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddControllers();

        AddProblemDetails(services);
        AddSwagger(services);
        AddPersistence(services, configuration);
        AddAuthentication(services, configuration, environment);
        AddAuthorization(services);
        AddCors(services, configuration, environment);
        AddRateLimiting(services);
        AddForwardedHeaders(services);
        AddPaperTradingOptions(services, configuration);

        services.AddHealthChecks();

        return services;
    }

    private static void AddProblemDetails(IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["timestampUtc"] = DateTime.UtcNow;
            };
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Trading System Dashboard API",
                    Version = "v1",
                    Description = "Operational and analytics API. Trading commands require Operator authorization and idempotency keys."
                });

            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });

            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        }
                    ] = []
                });
        });
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresTradingHistory(configuration);
        services.AddServiceHeartbeat(configuration, "DashboardApi");

        services.AddSingleton<PostgresDashboardStore>();
        services.AddSingleton<IDashboardQueryStore>(provider => provider.GetRequiredService<PostgresDashboardStore>());
        services.AddSingleton<IBotConfigurationStore>(provider => provider.GetRequiredService<PostgresDashboardStore>());
        services.AddSingleton<IBotCommandStore>(provider => provider.GetRequiredService<PostgresDashboardStore>());
        services.AddSingleton<IDashboardJobStore>(provider => provider.GetRequiredService<PostgresDashboardStore>());
        services.AddSingleton<IAlertCommandStore>(provider => provider.GetRequiredService<PostgresDashboardStore>());
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var signingKey =
            configuration["Authentication:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey) 
            || signingKey.Length < 32 
            || (!environment.IsDevelopment() && signingKey.StartsWith("DEVELOPMENT-ONLY", StringComparison.Ordinal)))
            throw new InvalidOperationException("Authentication:SigningKey must be a secure secret with at least 32 characters.");

        var issuer = configuration["Authentication:Issuer"] ?? "TradingDashboard";
        var audience = configuration["Authentication:Audience"] ?? "TradingDashboard";
        var adminUserName = configuration["Authentication:AdminUsername"];
        var adminPasswordHash = configuration["Authentication:AdminPasswordHash"];

        if (!environment.IsDevelopment() && (string.IsNullOrWhiteSpace(adminUserName) || adminUserName.Length > 100))
            throw new InvalidOperationException("Authentication:AdminUsername must be configured and be at most 100 characters outside Development.");

        if (!environment.IsDevelopment() &&
            (string.IsNullOrWhiteSpace(adminPasswordHash) ||
             !adminPasswordHash.StartsWith(
                 "PBKDF2-SHA256$",
                 StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Authentication:AdminPasswordHash must be configured as a PBKDF2-SHA256 hash outside Development.");
        }

        var lifetimeHours =
            Math.Clamp(
                configuration.GetValue(
                    "Authentication:TokenLifetimeHours",
                    12),
                1,
                24);

        services.AddSingleton(
    new DashboardCredentials(
        adminUserName?.Trim() ?? string.Empty,
        adminPasswordHash?.Trim() ?? string.Empty));

        services.AddSingleton(
            new DashboardTokenOptions(
                issuer,
                audience,
                signingKey,
                lifetimeHours));

        services.AddSingleton<
            DashboardTokenService>();

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata =
                    !environment.IsDevelopment();

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ClockSkew =
                            TimeSpan.FromSeconds(30),
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    signingKey)),
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
            });
    }

    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddAuthorization(opts =>
        {
            opts.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            opts.AddPolicy("Viewer", p => p.RequireRole("Viewer", "Operator", "Administrator"));
            opts.AddPolicy("Operator", p => p.RequireRole( "Operator", "Administrator"));
            opts.AddPolicy("Administrator", p => p.RequireRole( "Administrator"));
        });
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

        if (!environment.IsDevelopment() && corsOrigins.Length == 0)
            throw new InvalidOperationException("At least one explicit CORS origin is required outside Development.");

        services.AddCors(options =>
            options.AddPolicy(
                ReactCorsPolicy,
                policy =>
                {
                    policy
                        .WithOrigins(corsOrigins.Length == 0 ? ["http://localhost:5173"] : corsOrigins)
                        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                        .WithHeaders(
                            "Authorization",
                            "Content-Type",
                            CorrelationIdMiddleware.HeaderName,
                            IdempotencyMiddleware.HeaderName)
                        .WithExposedHeaders(
                            CorrelationIdMiddleware.HeaderName,
                            "X-Idempotent-Replay")
                        .SetPreflightMaxAge(
                            TimeSpan.FromHours(1));
                }));
    }

    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(opts =>
        {
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opts.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = "60";

                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Rate limit exceeded",
                        type: "https://trading-system/errors/rate-limit",
                        extensions:
                            new Dictionary<string, object?>
                            {
                                ["traceId"] = context.HttpContext.TraceIdentifier
                            })
                    .ExecuteAsync(context.HttpContext);
            };

            opts.AddPolicy(
                "auth",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

            opts.AddPolicy(
                "read",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetRateLimitKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 300,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

            opts.AddPolicy(
                "write",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetRateLimitKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

            opts.AddPolicy(
                "dangerous",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetRateLimitKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
        });
    }

    private static string GetRateLimitKey(HttpContext context)
        => context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

    private static void AddForwardedHeaders(IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(
            opts =>
            {
                opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                opts.ForwardLimit = 2;
            });
    }

    private static void AddPaperTradingOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PaperTradingOptions>().Bind(configuration.GetSection(PaperTradingOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<PaperTradingOptions>, PaperTradingOptionsValidator>();
    }
}
