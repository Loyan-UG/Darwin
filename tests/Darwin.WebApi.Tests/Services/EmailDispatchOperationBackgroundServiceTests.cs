using System.Net;
using System.Reflection;
using System.Text;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Notifications;
using Darwin.Infrastructure.Notifications.Brevo;
using Darwin.Infrastructure.Notifications.Smtp;
using Darwin.Worker;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Darwin.WebApi.Tests.Services;

public sealed class EmailDispatchOperationBackgroundServiceTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenDependenciesAreMissing()
    {
        var options = Options.Create(new EmailDispatchOperationWorkerOptions());
        var clock = new FixedClock(DateTime.UtcNow);

        Action noScopeFactory = () =>
            new EmailDispatchOperationBackgroundService(
                null!,
                options,
                clock,
                new Mock<ILogger<EmailDispatchOperationBackgroundService>>().Object);

        Action noOptions = () =>
            new EmailDispatchOperationBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                null!,
                clock,
                new Mock<ILogger<EmailDispatchOperationBackgroundService>>().Object);

        Action noClock = () =>
            new EmailDispatchOperationBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                options,
                null!,
                new Mock<ILogger<EmailDispatchOperationBackgroundService>>().Object);

        Action noLogger = () =>
            new EmailDispatchOperationBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                options,
                clock,
                null!);

        noScopeFactory.Should().Throw<ArgumentNullException>().WithParameterName("scopeFactory");
        noOptions.Should().Throw<ArgumentNullException>().WithParameterName("options");
        noClock.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        noLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ProcessAsync_Should_SendBrevoEmailDispatchOperation_AndCaptureProviderMessageId()
    {
        var nowUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(nowUtc);

        await using var db = EmailDispatchOperationBackgroundServiceTestDbContext.Create();
        SeedEmailSettings(db, sandboxMode: false);
        db.Set<EmailDispatchOperation>().Add(new EmailDispatchOperation
        {
            Id = Guid.NewGuid(),
            Provider = EmailProviderNames.Brevo,
            RecipientEmail = "user@darwin.de",
            Subject = "Welcome",
            HtmlBody = "<p>Welcome</p>",
            FlowKey = "WelcomeFlow",
            TemplateKey = "WelcomeTemplate",
            SenderRole = "Billing",
            CorrelationKey = "corr-brevo-1",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestHandler = new BrevoHttpHandler(
            new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("{\"messageId\":\"brevo-msg-1\"}", Encoding.UTF8, "application/json")
            });
        var httpClient = new HttpClient(requestHandler)
        {
            BaseAddress = new Uri("https://api.brevo.com/v3/")
        };

        var brevoSender = new BrevoEmailSender(
            httpClient,
            Options.Create(new BrevoEmailOptions
            {
                SenderEmail = "noreply@darwin.de"
            }),
            NullLogger<BrevoEmailSender>.Instance,
            db,
            clock);

        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(BrevoEmailSender))).Returns(brevoSender);

        var scope = new Mock<IServiceScope>(MockBehavior.Strict);
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
        scope.Setup(x => x.Dispose());

        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new EmailDispatchOperationBackgroundService(
            scopeFactory.Object,
            Options.Create(new EmailDispatchOperationWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            clock,
            new Mock<ILogger<EmailDispatchOperationBackgroundService>>().Object);

        await InvokeProcessAsync(
            service,
            new EmailDispatchOperationWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1,
                RetryCooldownSeconds = 30,
                PollIntervalSeconds = 15
            },
            TestContext.Current.CancellationToken);

        var operation = await db.Set<EmailDispatchOperation>().SingleAsync(TestContext.Current.CancellationToken);
        operation.Status.Should().Be("Succeeded");
        operation.AttemptCount.Should().Be(1);
        operation.LastAttemptAtUtc.Should().Be(nowUtc);
        operation.ProcessedAtUtc.Should().Be(nowUtc);
        operation.FailureReason.Should().BeNullOrWhiteSpace();

        var audit = await db.Set<EmailDispatchAudit>().SingleAsync(x => x.CorrelationKey == "corr-brevo-1", TestContext.Current.CancellationToken);
        audit.Provider.Should().Be(EmailProviderNames.Brevo);
        audit.SenderRole.Should().Be("Billing");
        audit.Status.Should().Be("Sent");
        audit.ProviderMessageId.Should().Be("brevo-msg-1");
        audit.CompletedAtUtc.Should().Be(nowUtc);
    }

    [Fact]
    public async Task ProcessAsync_Should_SendBrevoEmailDispatchOperation_WithSandboxModeHeader()
    {
        var nowUtc = new DateTime(2026, 1, 3, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(nowUtc);

        await using var db = EmailDispatchOperationBackgroundServiceTestDbContext.Create();
        SeedEmailSettings(db, sandboxMode: true);
        db.Set<EmailDispatchOperation>().Add(new EmailDispatchOperation
        {
            Id = Guid.NewGuid(),
            Provider = EmailProviderNames.Brevo,
            RecipientEmail = "user@darwin.de",
            Subject = "Welcome sandbox",
            HtmlBody = "<p>Welcome in sandbox</p>",
            FlowKey = "WelcomeFlow",
            TemplateKey = "WelcomeTemplate",
            CorrelationKey = "corr-brevo-sandbox-1",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestHandler = new BrevoCaptureRequestHandler(
            new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("{\"messageId\":\"brevo-msg-2\"}", Encoding.UTF8, "application/json")
            });
        var httpClient = new HttpClient(requestHandler)
        {
            BaseAddress = new Uri("https://api.brevo.com/v3/")
        };

        var brevoSender = new BrevoEmailSender(
            httpClient,
            Options.Create(new BrevoEmailOptions
            {
                SenderEmail = "noreply@darwin.de"
            }),
            NullLogger<BrevoEmailSender>.Instance,
            db,
            clock);

        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(BrevoEmailSender))).Returns(brevoSender);

        var scope = new Mock<IServiceScope>(MockBehavior.Strict);
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
        scope.Setup(x => x.Dispose());

        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new EmailDispatchOperationBackgroundService(
            scopeFactory.Object,
            Options.Create(new EmailDispatchOperationWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            clock,
            new Mock<ILogger<EmailDispatchOperationBackgroundService>>().Object);

        await InvokeProcessAsync(
            service,
            new EmailDispatchOperationWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1,
                RetryCooldownSeconds = 30,
                PollIntervalSeconds = 15
            },
            TestContext.Current.CancellationToken);

        var operation = await db.Set<EmailDispatchOperation>().SingleAsync(TestContext.Current.CancellationToken);
        operation.Status.Should().Be("Succeeded");
        operation.AttemptCount.Should().Be(1);

        var audit = await db.Set<EmailDispatchAudit>().SingleAsync(x => x.CorrelationKey == "corr-brevo-sandbox-1", TestContext.Current.CancellationToken);
        audit.Provider.Should().Be(EmailProviderNames.Brevo);
        audit.Status.Should().Be("Sent");
        audit.ProviderMessageId.Should().Be("brevo-msg-2");

        requestHandler.CapturedRequest.Should().NotBeNull();
        requestHandler.CapturedBody.Should().NotBeNullOrWhiteSpace();
        requestHandler.CapturedBody.Should().Contain("\"X-Sib-Sandbox\":\"drop\"");
        requestHandler.CapturedBody.Should().Contain("\"X-Correlation-Key\":\"corr-brevo-sandbox-1\"");
        requestHandler.CapturedBody.Should().Contain("\"sender\":{\"email\":\"no-reply@loyan.de\",\"name\":\"Loyan\"}");
        requestHandler.CapturedBody.Should().Contain("\"replyTo\":{\"email\":\"support@loyan.de\",\"name\":\"Loyan\"}");
    }

    [Fact]
    public async Task ProcessAsync_Should_FailLegacySmtpOperation_WhenSendThrowsAndLeaveRetryReadyState()
    {
        var nowUtc = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock(nowUtc);

        await using var db = EmailDispatchOperationBackgroundServiceTestDbContext.Create();
        SeedEmailSettings(db, sandboxMode: false);
        db.Set<EmailDispatchOperation>().Add(new EmailDispatchOperation
        {
            Id = Guid.NewGuid(),
            Provider = EmailProviderNames.Smtp,
            RecipientEmail = "bad-email",
            Subject = "SMTP test",
            HtmlBody = "<p>Failure path</p>",
            CorrelationKey = "corr-smtp-1",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var smtpSender = new SmtpEmailSender(
            Options.Create(new SmtpEmailOptions
            {
                Host = "unreachable.localhost",
                Port = 2525,
                FromAddress = "noreply@darwin.de"
            }),
            NullLogger<SmtpEmailSender>.Instance,
            db,
            clock);

        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(SmtpEmailSender))).Returns(smtpSender);

        var scope = new Mock<IServiceScope>(MockBehavior.Strict);
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
        scope.Setup(x => x.Dispose());

        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new EmailDispatchOperationBackgroundService(
            scopeFactory.Object,
            Options.Create(new EmailDispatchOperationWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            clock,
            new Mock<ILogger<EmailDispatchOperationBackgroundService>>().Object);

        await InvokeProcessAsync(
            service,
            new EmailDispatchOperationWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1,
                RetryCooldownSeconds = 30,
                PollIntervalSeconds = 15
            },
            TestContext.Current.CancellationToken);

        var operation = await db.Set<EmailDispatchOperation>().SingleAsync(TestContext.Current.CancellationToken);
        operation.Status.Should().Be("Failed");
        operation.AttemptCount.Should().Be(1);
        operation.LastAttemptAtUtc.Should().Be(nowUtc);
        operation.FailureReason.Should().NotBeNullOrWhiteSpace();

        var audit = await db.Set<EmailDispatchAudit>().SingleAsync(x => x.CorrelationKey == "corr-smtp-1", TestContext.Current.CancellationToken);
        audit.Provider.Should().Be(EmailProviderNames.Smtp);
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().NotBeNullOrWhiteSpace();
    }

    private static Task InvokeProcessAsync(
        EmailDispatchOperationBackgroundService service,
        EmailDispatchOperationWorkerOptions options,
        CancellationToken ct)
    {
        var method = typeof(EmailDispatchOperationBackgroundService).GetMethod(
            "ProcessAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var task = method!.Invoke(
            service,
            [options, ct]) as Task;
        task.Should().NotBeNull();
        return task!;
    }

    private static void SeedEmailSettings(EmailDispatchOperationBackgroundServiceTestDbContext db, bool sandboxMode)
    {
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Title = "Loyan",
            ContactEmail = "support@loyan.de",
            TransactionalEmailProvider = EmailProviderNames.Brevo,
            SupportEmail = "support@loyan.de",
            BillingEmail = "billing@loyan.de",
            NoReplyEmail = "no-reply@loyan.de",
            SystemAdminEmail = "dev@loyan.de",
            BrevoBaseUrl = "https://api.brevo.com/v3/",
            BrevoApiKey = "test-api-key",
            BrevoWebhookUsername = "webhook-user",
            BrevoWebhookPassword = "webhook-password",
            BrevoSandboxMode = sandboxMode
        });
        db.SaveChanges();
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class BrevoHttpHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Contain("smtp/email");
            return Task.FromResult(_response);
        }
    }

    private sealed class BrevoCaptureRequestHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response = response;
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Contain("smtp/email");
            CapturedBody = await request.Content!.ReadAsStringAsync(cancellationToken);

            return _response;
        }
    }

    private sealed class EmailDispatchOperationBackgroundServiceTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<EmailDispatchOperation> EmailDispatchOperations { get; set; } = null!;
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;
        public DbSet<Business> Businesses { get; set; } = null!;
        public DbSet<SiteSetting> SiteSettings { get; set; } = null!;

        private EmailDispatchOperationBackgroundServiceTestDbContext(
            DbContextOptions<EmailDispatchOperationBackgroundServiceTestDbContext> options)
            : base(options)
        {
        }

        public static EmailDispatchOperationBackgroundServiceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<EmailDispatchOperationBackgroundServiceTestDbContext>()
                .UseInMemoryDatabase($"darwin_email_dispatch_operation_worker_tests_{Guid.NewGuid()}")
                .Options;

            return new EmailDispatchOperationBackgroundServiceTestDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.DefaultTimeZoneId).IsRequired();
                builder.Ignore(x => x.Members);
                builder.Ignore(x => x.Locations);
                builder.Ignore(x => x.Favorites);
                builder.Ignore(x => x.Likes);
                builder.Ignore(x => x.Reviews);
                builder.Ignore(x => x.EngagementStats);
                builder.Ignore(x => x.Invitations);
                builder.Ignore(x => x.StaffQrCodes);
                builder.Ignore(x => x.Subscriptions);
                builder.Ignore(x => x.AnalyticsExportJobs);
            });

            modelBuilder.Entity<EmailDispatchOperation>(builder =>
            {
                builder.HasKey(x => x.Id);
            });

            modelBuilder.Entity<EmailDispatchAudit>(builder =>
            {
                builder.HasKey(x => x.Id);
            });

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
            });
        }
    }
}

