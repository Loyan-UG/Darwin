using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Integration;
using Darwin.Worker;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Darwin.WebApi.Tests.Services;

public sealed class WebhookDeliveryBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Use_Single_Utc_Snapshot_For_Delivery_Attempt_Timestamps()
    {
        var nowUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var laterUtc = nowUtc.AddMinutes(1);

        var clock = new Mock<IClock>(MockBehavior.Strict);
        clock.SetupSequence(x => x.UtcNow)
            .Returns(nowUtc)
            .Returns(laterUtc);

        await using var db = WebhookDeliveryWorkerTestDbContext.Create();
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "order.created",
            CallbackUrl = "https://example.com/webhook",
            Secret = "delivery-secret"
        };
        db.Set<WebhookSubscription>().Add(subscription);
        db.Set<WebhookDelivery>().AddRange(
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Status = "Pending"
            },
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Status = "Pending"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestHandler = new RecordingWebhookDeliveryHandler(
            new[] { HttpStatusCode.OK, HttpStatusCode.OK });
        var httpClientFactory = CreateHttpClientFactory(requestHandler);
        var service = CreateService(db, httpClientFactory, clock.Object);

        await InvokeAsync(
            service,
            new WebhookDeliveryWorkerOptions { Enabled = true, PollIntervalSeconds = 5, RetryCooldownSeconds = 5, MaxAttempts = 5 },
            TestContext.Current.CancellationToken);

        var deliveries = await db.Set<WebhookDelivery>().OrderBy(x => x.Id).ToListAsync(TestContext.Current.CancellationToken);
        deliveries.Should().AllSatisfy(d =>
        {
            d.Status.Should().Be("Succeeded");
            d.LastAttemptAtUtc.Should().Be(nowUtc);
            d.PayloadHash.Should().NotBeNullOrEmpty();
        });

        requestHandler.Requests.Select(x => ReadAttemptedAtUtc(x.PayloadJson)).Distinct().Should().Equal(nowUtc);
        requestHandler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RebuildPayload_And_Use_Fresh_Payload_Hash_OnRetry()
    {
        var firstAttemptUtc = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
        var secondAttemptUtc = firstAttemptUtc.AddMinutes(10);
        var deliveryId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var clock = new Mock<IClock>(MockBehavior.Strict);
        clock.SetupSequence(x => x.UtcNow)
            .Returns(firstAttemptUtc)
            .Returns(secondAttemptUtc);

        await using var db = WebhookDeliveryWorkerTestDbContext.Create();
        db.Set<WebhookSubscription>().Add(new WebhookSubscription
        {
            Id = subscriptionId,
            EventType = "order.updated",
            CallbackUrl = "https://example.com/webhook",
            Secret = "retry-secret"
        });
        db.Set<EventLog>().Add(new EventLog
        {
            Id = eventId,
            Type = "order.updated",
            OccurredAtUtc = new DateTime(2026, 1, 2, 9, 58, 0, DateTimeKind.Utc),
            IdempotencyKey = "evt-1",
            PropertiesJson = """{"note":"first-attempt"}"""
        });
        db.Set<WebhookDelivery>().Add(new WebhookDelivery
        {
            Id = deliveryId,
            SubscriptionId = subscriptionId,
            EventRefId = eventId,
            Status = "Pending",
            IdempotencyKey = "evt-delivery-1"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestHandler = new RecordingWebhookDeliveryHandler(new[]
        {
            HttpStatusCode.InternalServerError,
            HttpStatusCode.OK
        });
        var httpClientFactory = CreateHttpClientFactory(requestHandler);
        var service = CreateService(db, httpClientFactory, clock.Object);

        var options = new WebhookDeliveryWorkerOptions
        {
            Enabled = true,
            PollIntervalSeconds = 5,
            RetryCooldownSeconds = 5,
            MaxAttempts = 2
        };

        await InvokeAsync(service, options, TestContext.Current.CancellationToken);
        db.Set<WebhookDelivery>().Single(x => x.Id == deliveryId).Status.Should().Be("Failed");
        requestHandler.Requests.Should().HaveCount(1);

        var firstPayload = requestHandler.Requests.First().PayloadJson;
        var firstPayloadHash = ComputePayloadHash(firstPayload);
        var firstAttemptedAt = ReadAttemptedAtUtc(firstPayload);
        firstAttemptedAt.Should().Be(firstAttemptUtc);

        var eventLog = await db.Set<EventLog>().SingleAsync(x => x.Id == eventId, TestContext.Current.CancellationToken);
        eventLog.PropertiesJson = """{"note":"second-attempt"}""";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await InvokeAsync(service, options, TestContext.Current.CancellationToken);
        requestHandler.Requests.Should().HaveCount(2);

        var secondPayload = requestHandler.Requests.Last().PayloadJson;
        var secondPayloadHash = ComputePayloadHash(secondPayload);
        var secondAttemptedAt = ReadAttemptedAtUtc(secondPayload);
        var secondEnvelopeEventProperties = ReadEventProperties(secondPayload);

        firstPayload.Should().NotBe(secondPayload);
        firstPayloadHash.Should().NotBe(secondPayloadHash);
        secondAttemptedAt.Should().Be(secondAttemptUtc);
        secondEnvelopeEventProperties.Should().Be("""{"note":"second-attempt"}""");
        ReadPayloadHash(secondPayload).Should().BeNull();
        requestHandler.Requests.Last().Signature.Should().Be(ComputeSignature(secondPayload, "retry-secret"));

        var delivery = await db.Set<WebhookDelivery>().SingleAsync(x => x.Id == deliveryId, TestContext.Current.CancellationToken);
        delivery.PayloadHash.Should().Be(secondPayloadHash);
        delivery.Status.Should().Be("Succeeded");
        delivery.ResponseCode.Should().Be((int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessPendingDeliveriesAsync_Should_NotCallHttpClient_WhenClaimIsSkippedByConcurrentWorker()
    {
        var nowUtc = new DateTime(2026, 1, 3, 11, 0, 0, DateTimeKind.Utc);

        var databaseName = $"darwin_webhook_delivery_worker_tests_{Guid.NewGuid()}";
        await using var setupDb = WebhookDeliveryWorkerTestDbContext.Create(databaseName);
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "shipment.updated",
            CallbackUrl = "https://example.com/webhook",
            Secret = "multi-instance-secret"
        };
        setupDb.Set<WebhookSubscription>().Add(subscription);
        setupDb.Set<WebhookDelivery>().Add(new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Status = "Pending"
        });
        await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var db = WebhookDeliveryWorkerThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 1,
            failCompletionOnly: false,
            failWithConcurrency: true);

        var requestHandler = new RecordingWebhookDeliveryHandler(new[] { HttpStatusCode.OK });
        var service = CreateService(db, CreateHttpClientFactory(requestHandler), CreateFixedClock(nowUtc));
        var options = new WebhookDeliveryWorkerOptions
        {
            Enabled = true,
            PollIntervalSeconds = 5,
            RetryCooldownSeconds = 5,
            MaxAttempts = 5
        };

        var act = () => InvokeAsync(service, options, TestContext.Current.CancellationToken);

        await act();

        requestHandler.Requests.Should().BeEmpty();
        var delivery = await db.Set<WebhookDelivery>()
            .SingleAsync(TestContext.Current.CancellationToken);

        delivery.Status.Should().Be("Pending");
        delivery.RetryCount.Should().Be(0);
        delivery.LastAttemptAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_PersistResult_WhenFinalCompletionSaveRetriesTransientFailure()
    {
        var nowUtc = new DateTime(2026, 1, 4, 8, 0, 0, DateTimeKind.Utc);

        var databaseName = $"darwin_webhook_delivery_worker_tests_{Guid.NewGuid()}";
        await using var setupDb = WebhookDeliveryWorkerTestDbContext.Create(databaseName);
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "order.created",
            CallbackUrl = "https://example.com/webhook",
            Secret = "retry-secret"
        };
        setupDb.Set<WebhookSubscription>().Add(subscription);
        var deliveryId = Guid.NewGuid();
        setupDb.Set<WebhookDelivery>().Add(new WebhookDelivery
        {
            Id = deliveryId,
            SubscriptionId = subscription.Id,
            Status = "Pending"
        });
        await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var db = WebhookDeliveryWorkerThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 3,
            failCompletionOnly: true,
            failWithConcurrency: false);

        var requestHandler = new RecordingWebhookDeliveryHandler(new[] { HttpStatusCode.OK });
        var service = CreateService(db, CreateHttpClientFactory(requestHandler), CreateFixedClock(nowUtc));
        var options = new WebhookDeliveryWorkerOptions
        {
            Enabled = true,
            PollIntervalSeconds = 5,
            RetryCooldownSeconds = 5,
            MaxAttempts = 5
        };

        await InvokeAsync(service, options, TestContext.Current.CancellationToken);

        requestHandler.Requests.Should().HaveCount(1);
        var delivery = await db.Set<WebhookDelivery>().SingleAsync(x => x.Id == deliveryId, TestContext.Current.CancellationToken);

        delivery.Status.Should().Be("Succeeded");
        delivery.ResponseCode.Should().Be((int)HttpStatusCode.OK);
        delivery.RetryCount.Should().Be(1);
        delivery.LastAttemptAtUtc.Should().Be(nowUtc);
        delivery.PayloadHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_HandleFinalCompletionConcurrencyAndContinueProcessingOtherDeliveries()
    {
        var nowUtc = new DateTime(2026, 1, 4, 9, 0, 0, DateTimeKind.Utc);

        var databaseName = $"darwin_webhook_delivery_worker_tests_{Guid.NewGuid()}";
        await using var setupDb = WebhookDeliveryWorkerTestDbContext.Create(databaseName);
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "order.updated",
            CallbackUrl = "https://example.com/webhook",
            Secret = "concurrency-secret"
        };
        setupDb.Set<WebhookSubscription>().Add(subscription);
        setupDb.Set<WebhookDelivery>().AddRange(
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Status = "Pending"
            },
            new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Status = "Pending"
            });

        await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var db = WebhookDeliveryWorkerThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 3,
            failCompletionOnly: true,
            failWithConcurrency: true);

        var requestHandler = new RecordingWebhookDeliveryHandler(new[] { HttpStatusCode.OK, HttpStatusCode.OK });
        var service = CreateService(db, CreateHttpClientFactory(requestHandler), CreateFixedClock(nowUtc));
        var options = new WebhookDeliveryWorkerOptions
        {
            Enabled = true,
            PollIntervalSeconds = 5,
            RetryCooldownSeconds = 5,
            MaxAttempts = 5
        };

        await InvokeAsync(service, options, TestContext.Current.CancellationToken);

        requestHandler.Requests.Should().HaveCount(2);
        var deliveries = await db.Set<WebhookDelivery>()
            .OrderBy(x => x.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        deliveries.Should().AllSatisfy(d =>
        {
            d.Status.Should().Be("Succeeded");
            d.ResponseCode.Should().Be((int)HttpStatusCode.OK);
            d.RetryCount.Should().Be(1);
            d.PayloadHash.Should().NotBeNullOrEmpty();
            d.LastAttemptAtUtc.Should().Be(nowUtc);
        });
    }

    [Fact]
    public async Task ProcessPendingDeliveriesAsync_Should_RetryInactiveSubscriptionBatchSaveBeforeUsingFallbackPath()
    {
        var nowUtc = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc);
        var subscriptionId = Guid.NewGuid();

        var databaseName = $"darwin_webhook_delivery_worker_tests_{Guid.NewGuid()}";
        await using var setupDb = WebhookDeliveryWorkerTestDbContext.Create(databaseName);
        setupDb.Set<WebhookSubscription>().Add(new WebhookSubscription
        {
            Id = subscriptionId,
            EventType = "order.created",
            CallbackUrl = "https://example.com/webhook",
            Secret = "secret",
            IsActive = false
        });

        var deliveryIds = Enumerable.Range(0, 2)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        setupDb.Set<WebhookDelivery>().AddRange(
            new WebhookDelivery
            {
                Id = deliveryIds[0],
                SubscriptionId = subscriptionId,
                Status = "Pending"
            },
            new WebhookDelivery
            {
                Id = deliveryIds[1],
                SubscriptionId = subscriptionId,
                Status = "Pending"
            });

        await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var db = WebhookDeliveryWorkerThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 1,
            failCompletionOnly: false,
            failWithConcurrency: false);

        var requestHandler = new RecordingWebhookDeliveryHandler(new[] { HttpStatusCode.OK });
        var service = CreateService(db, CreateHttpClientFactory(requestHandler), CreateFixedClock(nowUtc));
        var options = new WebhookDeliveryWorkerOptions
        {
            Enabled = true,
            PollIntervalSeconds = 5,
            RetryCooldownSeconds = 5,
            MaxAttempts = 5
        };

        await InvokeAsync(service, options, TestContext.Current.CancellationToken);

        requestHandler.Requests.Should().BeEmpty();
        var deliveries = await db.Set<WebhookDelivery>()
            .Where(x => deliveryIds.Contains(x.Id))
            .OrderBy(x => x.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        deliveries.Should().AllSatisfy(d =>
        {
            d.Status.Should().Be("Failed");
            d.RetryCount.Should().Be(options.MaxAttempts);
            d.LastAttemptAtUtc.Should().Be(nowUtc);
            d.ResponseCode.Should().BeNull();
            d.PayloadHash.Should().BeNull();
        });
    }

    [Fact]
    public async Task ProcessPendingDeliveriesAsync_Should_PersistCompletionAndSkipResend_When_FinalCompletionSave_IsConcurrencyFailure()
    {
        var nowUtc = new DateTime(2026, 1, 6, 12, 0, 0, DateTimeKind.Utc);
        var databaseName = $"darwin_webhook_delivery_worker_tests_{Guid.NewGuid()}";

        var setupDb = WebhookDeliveryWorkerTestDbContext.Create(databaseName);
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            EventType = "order.created",
            CallbackUrl = "https://example.com/webhook",
            Secret = "concurrency-secret"
        };
        setupDb.Set<WebhookSubscription>().Add(subscription);
        var deliveryId = Guid.NewGuid();
        setupDb.Set<WebhookDelivery>().Add(new WebhookDelivery
        {
            Id = deliveryId,
            SubscriptionId = subscription.Id,
            Status = "Pending"
        });
        await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        await setupDb.DisposeAsync();

        await using var db = WebhookDeliveryWorkerThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 1,
            failCompletionOnly: true,
            failWithConcurrency: true);

        var requestHandler = new RecordingWebhookDeliveryHandler(new[] { HttpStatusCode.OK });
        var service = CreateService(db, CreateHttpClientFactory(requestHandler), CreateFixedClock(nowUtc));
        var options = new WebhookDeliveryWorkerOptions
        {
            Enabled = true,
            PollIntervalSeconds = 5,
            RetryCooldownSeconds = 5,
            MaxAttempts = 5
        };

        await InvokeAsync(service, options, TestContext.Current.CancellationToken);

        requestHandler.Requests.Should().HaveCount(1);
        var completed = await db.Set<WebhookDelivery>()
            .SingleAsync(x => x.Id == deliveryId, TestContext.Current.CancellationToken);
        completed.Status.Should().Be("Succeeded");
        completed.ResponseCode.Should().Be((int)HttpStatusCode.OK);
        completed.PayloadHash.Should().NotBeNullOrEmpty();
        completed.RetryCount.Should().Be(1);
        completed.LastAttemptAtUtc.Should().Be(nowUtc);

        await using var rerunDb = WebhookDeliveryWorkerTestDbContext.Create(databaseName);
        var rerunRequestHandler = new RecordingWebhookDeliveryHandler(new[] { HttpStatusCode.OK });
        var rerunService = CreateService(rerunDb, CreateHttpClientFactory(rerunRequestHandler), CreateFixedClock(nowUtc));
        await InvokeAsync(rerunService, options, TestContext.Current.CancellationToken);

        rerunRequestHandler.Requests.Should().BeEmpty();
    }

    private static async Task InvokeAsync(
        WebhookDeliveryBackgroundService service,
        WebhookDeliveryWorkerOptions options,
        CancellationToken cancellationToken)
    {
        var method = typeof(WebhookDeliveryBackgroundService).GetMethod("ProcessPendingDeliveriesAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        var task = (Task)method!.Invoke(service, [options, cancellationToken])!;
        await task.ConfigureAwait(false);
    }

    private static WebhookDeliveryBackgroundService CreateService(
        IAppDbContext db,
        IHttpClientFactory httpClientFactory,
        IClock clock)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        return new WebhookDeliveryBackgroundService(
            scopeFactory.Object,
            httpClientFactory,
            Options.Create(new WebhookDeliveryWorkerOptions()),
            clock,
            new Mock<ILogger<WebhookDeliveryBackgroundService>>().Object);
    }

    private static IHttpClientFactory CreateHttpClientFactory(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpClientFactory.Setup(x => x.CreateClient(nameof(WebhookDeliveryBackgroundService))).Returns(client);
        return httpClientFactory.Object;
    }

    private static IClock CreateFixedClock(DateTime utcNow)
    {
        var clock = new Mock<IClock>(MockBehavior.Strict);
        clock.Setup(x => x.UtcNow).Returns(utcNow);
        return clock.Object;
    }

    private static DateTime ReadAttemptedAtUtc(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var attempted = document.RootElement.GetProperty("attemptedAtUtc").GetString()!;
        return DateTime.Parse(attempted, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static string? ReadPayloadHash(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return document.RootElement.TryGetProperty("payloadHash", out var hashProperty)
            ? hashProperty.GetString()
            : null;
    }

    private static string ReadEventProperties(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return document.RootElement.GetProperty("event")
            .GetProperty("propertiesJson")
            .GetString()!;
    }

    private static string ComputeSignature(string payloadJson, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        using var hmac = new HMACSHA256(secretBytes);
        var signature = hmac.ComputeHash(payloadBytes);
        return $"sha256={Convert.ToHexString(signature).ToLowerInvariant()}";
    }

    private static string ComputePayloadHash(string payloadJson)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private sealed class RecordingWebhookDeliveryHandler(HttpStatusCode[] statusCodes) : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statusCodes = new(statusCodes);

        public List<(string PayloadJson, string? Signature)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = await request.Content!.ReadAsStringAsync(cancellationToken);
            var signature = request.Headers.TryGetValues("X-Darwin-Signature", out var values)
                ? values.SingleOrDefault()
                : null;

            Requests.Add((payload, signature));

            return new HttpResponseMessage(_statusCodes.TryDequeue(out var status) ? status : HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class WebhookDeliveryWorkerTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;
        public DbSet<WebhookDelivery> WebhookDeliveries { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;

        private WebhookDeliveryWorkerTestDbContext(DbContextOptions<WebhookDeliveryWorkerTestDbContext> options)
            : base(options)
        {
        }

        public static WebhookDeliveryWorkerTestDbContext Create()
        {
            return Create($"darwin_webhook_delivery_worker_tests_{Guid.NewGuid()}");
        }

        public static WebhookDeliveryWorkerTestDbContext Create(string databaseName)
        {
            var options = new DbContextOptionsBuilder<WebhookDeliveryWorkerTestDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new WebhookDeliveryWorkerTestDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();
    }

    private sealed class WebhookDeliveryWorkerThrowingSaveDbContext : DbContext, IAppDbContext
    {
        private readonly int _failFromCallNumber;
        private readonly bool _failCompletionOnly;
        private readonly bool _failWithConcurrency;
        private int _saveCallCount;
        private int _completionSaveCallCount;

        public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;
        public DbSet<WebhookDelivery> WebhookDeliveries { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;

        private WebhookDeliveryWorkerThrowingSaveDbContext(
            DbContextOptions<WebhookDeliveryWorkerThrowingSaveDbContext> options,
            int failFromCallNumber,
            bool failCompletionOnly,
            bool failWithConcurrency)
            : base(options)
        {
            _failFromCallNumber = failFromCallNumber;
            _failCompletionOnly = failCompletionOnly;
            _failWithConcurrency = failWithConcurrency;
        }

        public static WebhookDeliveryWorkerThrowingSaveDbContext Create(
            string databaseName,
            int failFromCallNumber,
            bool failCompletionOnly,
            bool failWithConcurrency)
        {
            var options = new DbContextOptionsBuilder<WebhookDeliveryWorkerThrowingSaveDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new WebhookDeliveryWorkerThrowingSaveDbContext(
                options,
                failFromCallNumber,
                failCompletionOnly,
                failWithConcurrency);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public override int SaveChanges()
        {
            if (ShouldFailSave())
            {
                ThrowFailure();
            }

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ShouldFailSave())
            {
                return Task.FromException<int>(CreateFailureException());
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        private bool ShouldFailSave()
        {
            _saveCallCount++;
            if (!_failCompletionOnly)
            {
                return _saveCallCount == _failFromCallNumber;
            }

            if (!IsCompletionSave())
            {
                return false;
            }

            _completionSaveCallCount++;
            return _completionSaveCallCount == _failFromCallNumber;
        }

        private bool IsCompletionSave()
        {
            return ChangeTracker.Entries<WebhookDelivery>()
                .Any(entry => entry.State != EntityState.Unchanged && !string.Equals(entry.Entity.Status, "Pending", StringComparison.OrdinalIgnoreCase));
        }

        private Exception CreateFailureException()
        {
            return _failWithConcurrency
                ? new DbUpdateConcurrencyException("simulated webhook delivery concurrency failure")
                : new DbUpdateException("simulated webhook delivery save failure");
        }

        private void ThrowFailure()
        {
            throw CreateFailureException();
        }
    }
}
