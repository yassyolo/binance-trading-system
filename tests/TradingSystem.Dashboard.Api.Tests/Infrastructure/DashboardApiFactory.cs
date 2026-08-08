using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using TradingSystem.Dashboard.Application.Contracts;
using TradingSystem.Dashboard.Application.Models;
using TradingSystem.Dashboard.Contracts.Models.Analytics;
using TradingSystem.EventStore.Contracts;
using TradingSystem.EventStore.TradingTimeline;
using TradingSystem.Operations.Contracts;
using TradingSystem.Operations.Models;
using TradingSystem.PaperTrading.Contracts;
using TradingSystem.ReplayEngine.Store;

namespace TradingSystem.Dashboard.Api.Tests.Infrastructure;

public sealed class DashboardApiFactory
    : WebApplicationFactory<Program>
{
    public Mock<IDashboardQueryStore> QueryStore { get; } = new();
    public Mock<IBotConfigurationStore> BotConfigurations { get; } = new();
    public Mock<IBotCommandStore> BotCommands { get; } = new();
    public Mock<IDashboardJobStore> Jobs { get; } = new();
    public Mock<IAlertCommandStore> AlertCommands { get; } = new();
    public Mock<ITradingTimelineReader> Timeline { get; } = new();
    public Mock<ITradingEventStore> EventStore { get; } = new();
    public Mock<IReplayJobStore> Replays { get; } = new();
    public Mock<IPaperTradingStore> PaperTrading { get; } = new();
    public Mock<IAuditLog> AuditLog { get; } = new();

    public DashboardApiFactory()
    {
        QueryStore
            .Setup(x => x.GetSignalsAsync(
                It.IsAny<DashboardQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        QueryStore
            .Setup(x => x.GetPositionsAsync(
                It.IsAny<DashboardQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        QueryStore
            .Setup(x => x.GetTradesAsync(
                It.IsAny<DashboardQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        QueryStore
            .Setup(x => x.GetRunsAsync(
                It.IsAny<DashboardQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        QueryStore
            .Setup(x => x.GetOptimizationTrialsAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        QueryStore
            .Setup(x => x.GetHealthAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        QueryStore
            .Setup(x => x.GetAlertsAsync(
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        QueryStore
            .Setup(x => x.GetAuditEventsAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        QueryStore
            .Setup(x => x.CompareRunsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StrategyComparisonDto?)null);

        BotConfigurations
            .Setup(x => x.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        BotCommands
            .Setup(x => x.GetAsync(
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Timeline
            .Setup(x => x.ReadAsync(
                It.IsAny<TradingSystem.EventStore.Models.EventStoreQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        EventStore
            .Setup(x => x.ReadStreamAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Replays
            .Setup(x => x.QueryAsync(
                It.IsAny<TradingSystem.ReplayEngine.Models.Enums.ReplayJobStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Replays
            .Setup(x => x.GetStepsAsync(
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        AuditLog
            .Setup(x => x.WriteAsync(
                It.IsAny<AuditEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:TradingHistory"] =
                            "Host=localhost;Port=55432;Database=trading;Username=test;Password=test",

                        ["TradingHistory:ConnectionStringName"] =
                            "TradingHistory",

                        ["TradingHistory:CommandTimeoutSeconds"] =
                            "5",

                        ["Authentication:Issuer"] =
                            "TradingDashboard",

                        ["Authentication:Audience"] =
                            "TradingDashboard",

                        ["Authentication:SigningKey"] =
                            "DEVELOPMENT-ONLY-DASHBOARD-TEST-SIGNING-KEY-1234567890",

                        ["Cors:Origins:0"] =
                            "http://localhost:5173",

                        ["ServiceHeartbeat:Enabled"] =
                            "false",

                        ["ServiceHeartbeat:Environment"] =
                            "Demo",

                        ["ServiceHeartbeat:IntervalSeconds"] =
                            "10",

                        ["ServiceHeartbeat:StaleAfterSeconds"] =
                            "30",

                        ["PaperTrading:Enabled"] =
                            "true",

                        ["PaperTrading:InitialBalance"] =
                            "10000",

                        ["PaperTrading:CommissionPercent"] =
                            "0.04",

                        ["PaperTrading:SlippagePercent"] =
                            "0.01",

                        ["PaperTrading:DefaultTakeProfitPercent"] =
                            "0.50",

                        ["PaperTrading:DefaultStopLossPercent"] =
                            "0.75",

                        ["PaperTrading:PricePollMilliseconds"] =
                            "1000",

                        ["PaperTrading:MaximumOpenPositions"] =
                            "100"
                    });
            });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll<IDashboardQueryStore>();
            services.RemoveAll<IBotConfigurationStore>();
            services.RemoveAll<IBotCommandStore>();
            services.RemoveAll<IDashboardJobStore>();
            services.RemoveAll<IAlertCommandStore>();
            services.RemoveAll<ITradingTimelineReader>();
            services.RemoveAll<ITradingEventStore>();
            services.RemoveAll<IReplayJobStore>();
            services.RemoveAll<IPaperTradingStore>();
            services.RemoveAll<IAuditLog>();

            services.AddSingleton(QueryStore.Object);
            services.AddSingleton(BotConfigurations.Object);
            services.AddSingleton(BotCommands.Object);
            services.AddSingleton(Jobs.Object);
            services.AddSingleton(AlertCommands.Object);
            services.AddSingleton(Timeline.Object);
            services.AddSingleton(EventStore.Object);
            services.AddSingleton(Replays.Object);
            services.AddSingleton(PaperTrading.Object);
            services.AddSingleton(AuditLog.Object);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme =
                        TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme =
                        TestAuthHandler.SchemeName;
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }

    public HttpClient CreateClient(
        string? role = null,
        string user = "integration-test-user")
    {
        var client = CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        if (!string.IsNullOrWhiteSpace(role))
        {
            client.DefaultRequestHeaders.Add(
                TestAuthHandler.RoleHeader,
                role);

            client.DefaultRequestHeaders.Add(
                TestAuthHandler.UserHeader,
                user);
        }

        return client;
    }
}
