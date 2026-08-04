using TradingSystem.PaperTrading;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TradingSystem.Dashboard.Api.Hardening;
using TradingSystem.Dashboard.Application;
using TradingSystem.Dashboard.Contracts;
using TradingSystem.Operations;
using TradingSystem.Persistence.PostgreSql.Configuration;
using TradingSystem.Persistence.PostgreSql.Dashboard;
using TradingSystem.EventStore;
using TradingSystem.ReplayEngine;
using TradingSystem.PaperTrading.Configuration;

var builder  =  WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options  => 
{
    options.CustomizeProblemDetails  =  context  => 
    {
        context.ProblemDetails.Extensions["traceId"]  =  context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["timestampUtc"]  =  DateTime.UtcNow;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options  => 
{
    options.SwaggerDoc("v1",  new OpenApiInfo
    {
        Title  =  "Trading System Dashboard API", 
        Version  =  "v1", 
        Description  =  "Operational and analytics API. Trading commands require Operator authorization and idempotency keys."
    });
    options.AddSecurityDefinition("Bearer",  new OpenApiSecurityScheme
    {
        Name  =  "Authorization", 
        Type  =  SecuritySchemeType.Http, 
        Scheme  =  "bearer", 
        BearerFormat  =  "JWT", 
        In  =  ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference  =  new OpenApiReference { Type  =  ReferenceType.SecurityScheme,  Id  =  "Bearer" } }]  =  []
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddPostgresTradingHistory(builder.Configuration);
builder.Services.AddServiceHeartbeat(builder.Configuration,  "DashboardApi");

builder.Services.AddSingleton<PostgresDashboardStore>();
builder.Services.AddSingleton<IDashboardQueryStore>(services  =>  services.GetRequiredService<PostgresDashboardStore>());
builder.Services.AddSingleton<IBotConfigurationStore>(services  =>  services.GetRequiredService<PostgresDashboardStore>());
builder.Services.AddSingleton<IBotCommandStore>(services  =>  services.GetRequiredService<PostgresDashboardStore>());
builder.Services.AddSingleton<IDashboardJobStore>(services  =>  services.GetRequiredService<PostgresDashboardStore>());
builder.Services.AddSingleton<IAlertCommandStore>(services  =>  services.GetRequiredService<PostgresDashboardStore>());

var signingKey  =  builder.Configuration["Authentication:SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey)  ||  signingKey.Length < 32  || 
    (!builder.Environment.IsDevelopment()  &&  signingKey.StartsWith("DEVELOPMENT-ONLY",  StringComparison.Ordinal)))
    throw new InvalidOperationException("Authentication:SigningKey must be a secure secret with at least 32 characters.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options  => 
    {
        options.MapInboundClaims  =  false;
        options.RequireHttpsMetadata  =  !builder.Environment.IsDevelopment();
        options.TokenValidationParameters  =  new TokenValidationParameters
        {
            ValidateIssuer  =  true, 
            ValidateAudience  =  true, 
            ValidateLifetime  =  true, 
            ValidateIssuerSigningKey  =  true, 
            RequireExpirationTime  =  true, 
            RequireSignedTokens  =  true, 
            ClockSkew  =  TimeSpan.FromSeconds(30), 
            ValidIssuer  =  builder.Configuration["Authentication:Issuer"] ?? "TradingDashboard", 
            ValidAudience  =  builder.Configuration["Authentication:Audience"] ?? "TradingDashboard", 
            IssuerSigningKey  =  new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)), 
            NameClaimType  =  ClaimTypes.Name, 
            RoleClaimType  =  ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options  => 
{
    options.FallbackPolicy  =  new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("Viewer",  policy  =>  policy.RequireRole("Viewer",  "Operator",  "Administrator"));
    options.AddPolicy("Operator",  policy  =>  policy.RequireRole("Operator",  "Administrator"));
    options.AddPolicy("Administrator",  policy  =>  policy.RequireRole("Administrator"));
});

var corsOrigins  =  builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
if (!builder.Environment.IsDevelopment()  &&  corsOrigins.Length == 0)
    throw new InvalidOperationException("At least one explicit CORS origin is required outside Development.");

builder.Services.AddCors(options  =>  options.AddPolicy("React",  policy  => 
{
    policy.WithOrigins(corsOrigins.Length == 0 ? ["http://localhost:5173"] : corsOrigins)
        .WithMethods("GET",  "POST",  "PUT",  "PATCH",  "DELETE")
        .WithHeaders("Authorization",  "Content-Type",  CorrelationIdMiddleware.HeaderName,  IdempotencyMiddleware.HeaderName)
        .WithExposedHeaders(CorrelationIdMiddleware.HeaderName,  "X-Idempotent-Replay")
        .SetPreflightMaxAge(TimeSpan.FromHours(1));
}));

builder.Services.AddRateLimiter(options  => 
{
    options.RejectionStatusCode  =  StatusCodes.Status429TooManyRequests;
    options.OnRejected  =  async (context,  ct)  => 
    {
        context.HttpContext.Response.Headers["Retry-After"]  =  "60";
        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests, 
            title: "Rate limit exceeded", 
            type: "https://trading-system/errors/rate-limit", 
            extensions: new Dictionary<string,  object?> { ["traceId"]  =  context.HttpContext.TraceIdentifier })
            .ExecuteAsync(context.HttpContext);
    };
    options.AddPolicy("read",  context  =>  RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous", 
        _  =>  new FixedWindowRateLimiterOptions { PermitLimit  =  300,  Window  =  TimeSpan.FromMinutes(1),  QueueLimit  =  0 }));
    options.AddPolicy("write",  context  =>  RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous", 
        _  =>  new FixedWindowRateLimiterOptions { PermitLimit  =  30,  Window  =  TimeSpan.FromMinutes(1),  QueueLimit  =  0 }));
    options.AddPolicy("dangerous",  context  =>  RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous", 
        _  =>  new FixedWindowRateLimiterOptions { PermitLimit  =  10,  Window  =  TimeSpan.FromMinutes(1),  QueueLimit  =  0 }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options  => 
{
    options.ForwardedHeaders  =  ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit  =  2;
});
builder.WebHost.ConfigureKestrel(options  =>  options.Limits.MaxRequestBodySize  =  1_048_576);

builder.Services.AddOptions<PaperTradingOptions>().Bind(builder.Configuration.GetSection(PaperTradingOptions.SectionName));

var app  =  builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors("React");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<AuditMiddleware>();

if (app.Environment.IsDevelopment()  ||  builder.Configuration.GetValue("Swagger:Enabled",  false))
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseWhen(context  =>  context.Request.Path.StartsWithSegments("/swagger"),  branch  => 
        {
            branch.Use(async (context,  next)  => 
            {
                if (context.User.Identity?.IsAuthenticated !=  true  ||  !context.User.IsInRole("Administrator"))
                {
                    context.Response.StatusCode  =  StatusCodes.Status404NotFound;
                    return;
                }
                await next(context);
            });
        });
    }
    app.UseSwagger();
    app.UseSwaggerUI(options  =>  options.DisplayRequestDuration());
}

string UserName(HttpContext context)  =>  context.User.Identity?.Name ?? context.User.FindFirstValue("sub") ?? "dashboard-user";
var api  =  app.MapGroup("/api/v1").WithTags("Dashboard");

api.MapGet("/timeline",  async (string? aggregateType,  string? aggregateId,  string? botName,  string? symbol,  string? positionId,  string? signalId,  string? correlationId,  string? eventType,  DateTime? fromUtc,  DateTime? toUtc,  long? afterGlobalPosition,  int skip,  int take,  ITradingTimelineReader timeline,  CancellationToken ct)  => 
    await timeline.ReadAsync(new EventStoreQuery(aggregateType,  aggregateId,  botName,  symbol,  positionId,  signalId,  correlationId,  eventType,  fromUtc,  toUtc,  afterGlobalPosition,  RequestValidation.Skip(skip),  RequestValidation.PageSize(take)),  ct))
    .RequireAuthorization("Viewer").RequireRateLimiting("read");

api.MapGet("/event-streams/{aggregateType}/{aggregateId}",  async (string aggregateType,  string aggregateId,  long afterVersion,  int take,  ITradingEventStore store,  CancellationToken ct)  => 
    await store.ReadStreamAsync(aggregateType,  aggregateId,  Math.Max(0,  afterVersion),  Math.Clamp(take,  1,  500),  ct))
    .RequireAuthorization("Viewer").RequireRateLimiting("read");

api.MapPost("/replays",  async (CreateReplayRequest request,  HttpContext http,  IReplayJobStore store,  CancellationToken ct)  => 
{
    if (string.IsNullOrWhiteSpace(request.Name)  ||  request.Name.Length > 150)
        throw new ApiValidationException(new Dictionary<string,  string[]> { ["name"]  =  ["Replay name is required and must be at most 150 characters."] });
    if (request.FromGlobalPosition.HasValue  &&  request.ToGlobalPosition.HasValue  &&  request.FromGlobalPosition > request.ToGlobalPosition)
        throw new ApiValidationException(new Dictionary<string,  string[]> { ["range"]  =  ["FromGlobalPosition must not be greater than ToGlobalPosition."] });
    if (request.FromUtc.HasValue  &&  request.ToUtc.HasValue  &&  request.FromUtc >= request.ToUtc)
        throw new ApiValidationException(new Dictionary<string,  string[]> { ["range"]  =  ["FromUtc must be earlier than ToUtc."] });
    if (request.Mode == ReplayMode.StrategyComparison  &&  (string.IsNullOrWhiteSpace(request.CandidateStrategyPluginId)  ||  string.IsNullOrWhiteSpace(request.CandidateStrategyVersion)))
        throw new ApiValidationException(new Dictionary<string,  string[]> { ["candidateStrategy"]  =  ["Strategy comparison requires candidate plugin id and version."] });
    var id  =  await store.EnqueueAsync(request with { BatchSize  =  Math.Clamp(request.BatchSize,  1,  1000) },  UserName(http),  ct);
    return Results.Accepted($"/api/v1/replays/{id}",  new { replayId  =  id });
}).RequireAuthorization("Operator").RequireRateLimiting("write");

api.MapGet("/replays",  (ReplayJobStatus? status,  int skip,  int take,  IReplayJobStore store,  CancellationToken ct)  => 
    store.QueryAsync(status,  RequestValidation.Skip(skip),  RequestValidation.PageSize(take),  ct))
    .RequireAuthorization("Viewer").RequireRateLimiting("read");

api.MapGet("/replays/{replayId:guid}",  async (Guid replayId,  IReplayJobStore store,  CancellationToken ct)  => 
    await store.GetAsync(replayId,  ct) is { } job ? Results.Ok(job) : Results.NotFound())
    .RequireAuthorization("Viewer").RequireRateLimiting("read");

api.MapGet("/replays/{replayId:guid}/result",  async (Guid replayId,  IReplayJobStore store,  CancellationToken ct)  => 
    await store.GetSummaryAsync(replayId,  ct) is { } summary ? Results.Ok(summary) : Results.NotFound())
    .RequireAuthorization("Viewer").RequireRateLimiting("read");

api.MapGet("/replays/{replayId:guid}/steps",  (Guid replayId,  long afterGlobalPosition,  int take,  IReplayJobStore store,  CancellationToken ct)  => 
    store.GetStepsAsync(replayId,  Math.Max(0,  afterGlobalPosition),  Math.Clamp(take,  1,  500),  ct))
    .RequireAuthorization("Viewer").RequireRateLimiting("read");

api.MapPost("/replays/{replayId:guid}/cancel",  async (Guid replayId,  HttpContext http,  IReplayJobStore store,  CancellationToken ct)  => 
{
    await store.CancelAsync(replayId,  UserName(http),  ct);
    return Results.Accepted();
}).RequireAuthorization("Operator").RequireRateLimiting("write");

api.MapGet("/overview",  (IDashboardQueryStore store,  CancellationToken ct)  =>  store.GetOverviewAsync(ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/bots",  (IBotConfigurationStore store,  CancellationToken ct)  =>  store.GetAllAsync(ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/bots/{botName}",  (string botName,  IBotConfigurationStore store,  CancellationToken ct)  =>  { RequestValidation.ValidateBotName(botName); return store.GetAsync(botName,  ct); }).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapPut("/bots/{botName}/configuration",  async (string botName,  UpdateBotConfigurationRequest request,  HttpContext http,  IBotConfigurationStore store,  CancellationToken ct)  => 
{
    RequestValidation.ValidateBotName(botName); RequestValidation.Validate(request);
    try { return Results.Ok(await store.UpdateAsync(botName,  request,  UserName(http),  ct)); }
    catch (InvalidOperationException exception) { throw new ApiConflictException(exception.Message); }
}).RequireAuthorization("Operator").RequireRateLimiting("write");
api.MapPost("/bots/{botName}/commands",  async (string botName,  BotCommandRequest request,  HttpContext http,  IBotCommandStore store,  CancellationToken ct)  => 
{
    RequestValidation.ValidateBotName(botName); RequestValidation.Validate(request);
    return Results.Accepted(value: await store.EnqueueAsync(botName,  request,  UserName(http),  ct));
}).RequireAuthorization("Operator").RequireRateLimiting("dangerous");
api.MapGet("/commands",  (string? botName,  int take,  IBotCommandStore store,  CancellationToken ct)  => 
{
    if (botName is not null) RequestValidation.ValidateBotName(botName);
    return store.GetAsync(botName,  RequestValidation.PageSize(take),  ct);
}).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/signals",  (string? botName,  string? symbol,  DateTime? fromUtc,  DateTime? toUtc,  int skip,  int take,  IDashboardQueryStore store,  CancellationToken ct)  => 
    store.GetSignalsAsync(new(RequestValidation.Skip(skip),  RequestValidation.PageSize(take),  botName,  symbol,  fromUtc,  toUtc),  ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/positions",  (string? botName,  string? symbol,  string? status,  int skip,  int take,  IDashboardQueryStore store,  CancellationToken ct)  => 
    store.GetPositionsAsync(new(RequestValidation.Skip(skip),  RequestValidation.PageSize(take),  botName,  symbol,  Status: status),  ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/trades",  (string? botName,  string? symbol,  int skip,  int take,  IDashboardQueryStore store,  CancellationToken ct)  => 
    store.GetTradesAsync(new(RequestValidation.Skip(skip),  RequestValidation.PageSize(take),  botName,  symbol),  ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/analytics",  (string? botName,  string? symbol,  DateTime? fromUtc,  DateTime? toUtc,  IDashboardQueryStore store,  CancellationToken ct)  => 
    store.GetAnalyticsAsync(new(BotName: botName,  Symbol: symbol,  FromUtc: fromUtc,  ToUtc: toUtc),  ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/analytics/equity",  (string? botName,  IDashboardQueryStore store,  CancellationToken ct)  =>  store.GetEquityAsync(new(BotName: botName),  ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/charts/{symbol}/{interval}",  (string symbol,  string interval,  DateTime fromUtc,  DateTime toUtc,  IDashboardQueryStore store,  CancellationToken ct)  => 
{
    RequestValidation.ValidateChart(symbol,  interval,  fromUtc,  toUtc);
    return store.GetPriceChartAsync(symbol,  interval,  fromUtc,  toUtc,  ct);
}).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapPost("/backtests",  async (BacktestRequest request,  HttpContext http,  IDashboardJobStore store,  CancellationToken ct)  => 
{
    RequestValidation.Validate(request);
    return Results.Accepted(value: await store.EnqueueBacktestAsync(request,  UserName(http),  ct));
}).RequireAuthorization("Operator").RequireRateLimiting("write");
api.MapPost("/optimizations",  async (OptimizationRequest request,  HttpContext http,  IDashboardJobStore store,  CancellationToken ct)  => 
{
    RequestValidation.Validate(request);
    return Results.Accepted(value: await store.EnqueueOptimizationAsync(request,  UserName(http),  ct));
}).RequireAuthorization("Operator").RequireRateLimiting("write");
api.MapGet("/runs",  (string? botName,  int skip,  int take,  IDashboardQueryStore store,  CancellationToken ct)  =>  store.GetRunsAsync(new(RequestValidation.Skip(skip),  RequestValidation.PageSize(take),  botName),  ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/optimizations/{runId:guid}/trials",  (Guid runId,  int take,  IDashboardQueryStore store,  CancellationToken ct)  =>  store.GetOptimizationTrialsAsync(runId,  RequestValidation.PageSize(take),  ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/comparisons",  async (Guid leftRunId,  Guid rightRunId,  IDashboardQueryStore store,  CancellationToken ct)  =>  await store.CompareRunsAsync(leftRunId,  rightRunId,  ct) is { } comparison ? Results.Ok(comparison) : Results.NotFound()).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/health/components",  (IDashboardQueryStore store,  CancellationToken ct)  =>  store.GetHealthAsync(ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/alerts",  (bool acknowledged,  int take,  IDashboardQueryStore store,  CancellationToken ct)  =>  store.GetAlertsAsync(acknowledged,  RequestValidation.PageSize(take),  ct)).RequireAuthorization("Viewer").RequireRateLimiting("read");
api.MapGet("/audit",  (string? actor,  string? action,  DateTime? fromUtc,  DateTime? toUtc,  int skip,  int take,  IDashboardQueryStore store,  CancellationToken ct)  => 
    store.GetAuditEventsAsync(actor,  action,  fromUtc,  toUtc,  RequestValidation.Skip(skip),  RequestValidation.PageSize(take),  ct)).RequireAuthorization("Operator").RequireRateLimiting("read");
api.MapPost("/alerts/{id:long}/acknowledge",  async (long id,  HttpContext http,  IAlertCommandStore store,  CancellationToken ct)  => 
{
    if (id <= 0) throw new ApiValidationException(new Dictionary<string,  string[]> { ["id"]  =  ["Alert id must be positive."] });
    await store.AcknowledgeAsync(id,  UserName(http),  ct); return Results.NoContent();
}).RequireAuthorization("Operator").RequireRateLimiting("write");


api.MapGet("/paper/account",  async (IPaperTradingStore store,  IOptions<PaperTradingOptions> options,  CancellationToken ct)  => 
    Results.Ok(await store.GetAccountAsync(options.Value.InitialBalance,  ct)))
    .RequireAuthorization("Viewer").RequireRateLimiting("read");

api.MapGet("/paper/positions",  async (string? botName,  string? symbol,  PaperPositionStatus? status,  int? skip,  int? take,  IPaperTradingStore store,  CancellationToken ct)  => 
    Results.Ok(await store.QueryAsync(botName,  symbol,  status,  Math.Max(0,  skip ?? 0),  Math.Clamp(take ?? 100,  1,  500),  ct)))
    .RequireAuthorization("Viewer").RequireRateLimiting("read");

api.MapPost("/paper/reset",  async (HttpContext context,  IPaperTradingStore store,  CancellationToken ct)  => 
{
    await store.ResetAsync(UserName(context),  ct);
    return Results.Accepted();
}).RequireAuthorization("Administrator").RequireRateLimiting("dangerous");

app.MapHealthChecks("/health/live").AllowAnonymous().DisableRateLimiting();
app.Run();
