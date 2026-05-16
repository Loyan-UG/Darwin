using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Unit tests for <see cref="GetProviderCallbackInboxPageHandler"/>, covering filtering,
/// summary calculations, payload preview building, and stale-pending logic.
/// </summary>
public sealed class ProviderCallbackInboxHandlerTests
{
    private static readonly DateTime FixedNow = new(2030, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private GetProviderCallbackInboxPageHandler CreateHandler(InboxTestDbContext db) =>
        new(db, new FixedClock(FixedNow));

    private static ProviderCallbackInboxMessage MakeMessage(
        string provider = "Stripe",
        string callbackType = "payment_intent.succeeded",
        string status = "Pending",
        string? idempotencyKey = null,
        string payloadJson = "{}",
        DateTime? createdAt = null,
        string? failureReason = null,
        bool isDeleted = false,
        int attemptCount = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            CallbackType = callbackType,
            Status = status,
            IdempotencyKey = idempotencyKey,
            PayloadJson = payloadJson,
            CreatedAtUtc = createdAt ?? FixedNow.AddMinutes(-5),
            FailureReason = failureReason,
            IsDeleted = isDeleted,
            AttemptCount = attemptCount,
            RowVersion = [1]
        };

    // ─── Basic paging and soft-delete exclusion ───────────────────────────────

    [Fact]
    public async Task GetInboxPage_Should_ReturnEmpty_WhenNoMessages()
    {
        await using var db = InboxTestDbContext.Create();
        var handler = CreateHandler(db);

        var (items, total, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetInboxPage_Should_ExcludeSoftDeletedMessages()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(isDeleted: true),
            MakeMessage(isDeleted: false)
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        total.Should().Be(1, "soft-deleted messages must not be returned");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetInboxPage_Should_NormalizePage_WhenPageIsZeroOrNegative()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(MakeMessage());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total, _, _) = await handler.HandleAsync(
            0, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        total.Should().Be(1, "page is clamped to 1 when zero or negative");
    }

    // ─── Filters ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInboxPage_Should_Filter_ByProvider()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(provider: "Stripe"),
            MakeMessage(provider: "DHL")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto { Provider = "Stripe" },
            TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Provider.Should().Be("Stripe");
    }

    [Fact]
    public async Task GetInboxPage_Should_Filter_ByStatus_Pending()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(status: "Pending"),
            MakeMessage(status: "Failed")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto { Status = "Pending" },
            TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetInboxPage_Should_NormalizeStatus_SucceededToProcessed()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(MakeMessage(status: "Succeeded"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(),
            TestContext.Current.CancellationToken);

        items.Single().Status.Should().Be("Processed",
            "'Succeeded' is normalized to 'Processed' in the projection");
    }

    [Fact]
    public async Task GetInboxPage_Should_Filter_ProcessedStatus_IncludesSucceeded()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(status: "Processed"),
            MakeMessage(status: "Succeeded"),
            MakeMessage(status: "Pending")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (_, total, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto { Status = "Processed" },
            TestContext.Current.CancellationToken);

        total.Should().Be(2, "filter 'Processed' should also match 'Succeeded'");
    }

    [Fact]
    public async Task GetInboxPage_Should_Filter_FailedOnly()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(status: "Pending"),
            MakeMessage(status: "Failed"),
            MakeMessage(status: "Failed")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto { FailedOnly = true },
            TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().AllSatisfy(i => i.Status.Should().Be("Failed"));
    }

    [Fact]
    public async Task GetInboxPage_Should_Filter_StalePendingOnly()
    {
        await using var db = InboxTestDbContext.Create();
        // Stale: created > 30min ago and still Pending
        // Fresh: created 10min ago
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(status: "Pending", createdAt: FixedNow.AddMinutes(-31)), // stale
            MakeMessage(status: "Pending", createdAt: FixedNow.AddMinutes(-10)), // fresh
            MakeMessage(status: "Failed", createdAt: FixedNow.AddMinutes(-60))   // not pending
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto { StalePendingOnly = true },
            TestContext.Current.CancellationToken);

        total.Should().Be(1, "only pending messages older than 30min are stale");
        items.Single().IsStalePending.Should().BeTrue();
    }

    [Fact]
    public async Task GetInboxPage_Should_Filter_DeliveryFailureOnly_ForBrevo()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(provider: "Brevo", callbackType: "hard_bounce"),
            MakeMessage(provider: "Brevo", callbackType: "delivered"), // not a delivery failure
            MakeMessage(provider: "Stripe", callbackType: "hard_bounce")  // wrong provider filter
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto { DeliveryFailureOnly = true },
            TestContext.Current.CancellationToken);

        total.Should().Be(1, "only Brevo delivery failure events match the filter");
        items.Single().CallbackType.Should().Be("hard_bounce");
    }

    // ─── Summary calculations ─────────────────────────────────────────────────

    [Fact]
    public async Task GetInboxPage_Should_ReturnZeroSummary_WhenEmpty()
    {
        await using var db = InboxTestDbContext.Create();
        var handler = CreateHandler(db);

        var (_, _, summary, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.PendingCount.Should().Be(0);
        summary.FailedCount.Should().Be(0);
        summary.ProcessedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetInboxPage_Should_ComputeSummary_Correctly()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(provider: "Brevo", status: "Pending", callbackType: "delivered"),
            MakeMessage(provider: "Brevo", status: "Failed", callbackType: "hard_bounce"),
            MakeMessage(provider: "Stripe", status: "Processed"),
            MakeMessage(provider: "Stripe", status: "Succeeded"),
            MakeMessage(provider: "Stripe", status: "Pending", attemptCount: 2)
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (_, _, summary, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(5);
        summary.PendingCount.Should().Be(2);
        summary.FailedCount.Should().Be(1);
        summary.ProcessedCount.Should().Be(2, "Processed and Succeeded both count");
        summary.RetriedCount.Should().Be(1, "only the message with AttemptCount > 0 counts");
        summary.BrevoTotalCount.Should().Be(2);
        summary.BrevoPendingCount.Should().Be(1);
        summary.BrevoFailedCount.Should().Be(1);
        summary.BrevoDeliveryFailureEventCount.Should().Be(1);
    }

    // ─── Providers list ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetInboxPage_Should_Return_DistinctProviders()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            MakeMessage(provider: "Stripe"),
            MakeMessage(provider: "Stripe"),
            MakeMessage(provider: "DHL"),
            MakeMessage(provider: "Brevo")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (_, _, _, providers) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        providers.Should().BeEquivalentTo(["Brevo", "DHL", "Stripe"], o => o.WithStrictOrdering(),
            "providers are returned in ascending alphabetical order");
    }

    // ─── Payload preview: Stripe ──────────────────────────────────────────────

    [Fact]
    public async Task GetInboxPage_Should_BuildStripePayloadPreview_WithEventAndType()
    {
        await using var db = InboxTestDbContext.Create();
        var payload = """{"id":"evt_123","type":"payment_intent.succeeded","created":1696147200}""";
        db.Set<ProviderCallbackInboxMessage>().Add(MakeMessage(provider: "Stripe", payloadJson: payload));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        var preview = items.Single().PayloadPreview;
        preview.Should().Contain("evt_123");
        preview.Should().Contain("payment_intent.succeeded");
    }

    [Fact]
    public async Task GetInboxPage_Should_ReturnFallbackPreview_ForInvalidJson()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(MakeMessage(payloadJson: "not-json"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        items.Single().PayloadPreview.Should().Be("Payload captured; preview unavailable.",
            "invalid JSON should return the standard fallback message");
    }

    [Fact]
    public async Task GetInboxPage_Should_ReturnEmptyPreview_ForEmptyPayloadJson()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(MakeMessage(payloadJson: ""));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        items.Single().PayloadPreview.Should().BeEmpty("empty payload JSON has no preview");
    }

    // ─── Payload preview: Brevo ───────────────────────────────────────────────

    [Fact]
    public async Task GetInboxPage_Should_MaskEmailInBrevoPreview()
    {
        await using var db = InboxTestDbContext.Create();
        var payload = """{"event":"hard_bounce","email":"john.doe@example.com","message-id":"mid_001"}""";
        db.Set<ProviderCallbackInboxMessage>().Add(MakeMessage(provider: "Brevo", payloadJson: payload));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        var preview = items.Single().PayloadPreview;
        preview.Should().NotContain("john.doe@example.com", "email must be masked");
        preview.Should().Contain("***@example.com", "masked email must appear with visible domain");
        preview.Should().Contain("hard_bounce");
    }

    // ─── Payload preview: DHL ─────────────────────────────────────────────────

    [Fact]
    public async Task GetInboxPage_Should_BuildDhlPayloadPreview_WithShipmentAndTracking()
    {
        await using var db = InboxTestDbContext.Create();
        var payload = """{"providerShipmentReference":"REF-001","trackingNumber":"1234567890","carrierEventKey":"DELIVERED"}""";
        db.Set<ProviderCallbackInboxMessage>().Add(MakeMessage(provider: "DHL", payloadJson: payload));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        var preview = items.Single().PayloadPreview;
        preview.Should().Contain("REF-001");
        preview.Should().Contain("1234567890");
        preview.Should().Contain("DELIVERED");
    }

    // ─── Age and stale flag ───────────────────────────────────────────────────

    [Fact]
    public async Task GetInboxPage_Should_ComputeAgeMinutes_Correctly()
    {
        await using var db = InboxTestDbContext.Create();
        var createdAt = FixedNow.AddMinutes(-45);
        db.Set<ProviderCallbackInboxMessage>().Add(MakeMessage(createdAt: createdAt, status: "Pending"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        items.Single().AgeMinutes.Should().Be(45);
    }

    [Fact]
    public async Task GetInboxPage_Should_MarkIsStalePending_True_WhenPendingAndOlderThan30Min()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(
            MakeMessage(status: "Pending", createdAt: FixedNow.AddMinutes(-31)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        items.Single().IsStalePending.Should().BeTrue();
    }

    [Fact]
    public async Task GetInboxPage_Should_MarkIsStalePending_False_WhenPendingButRecent()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(
            MakeMessage(status: "Pending", createdAt: FixedNow.AddMinutes(-5)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        items.Single().IsStalePending.Should().BeFalse();
    }

    [Fact]
    public async Task GetInboxPage_Should_MarkIsStalePending_False_WhenFailedAndOld()
    {
        await using var db = InboxTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(
            MakeMessage(status: "Failed", createdAt: FixedNow.AddHours(-5)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _, _, _) = await handler.HandleAsync(
            1, 20, new ProviderCallbackInboxFilterDto(), TestContext.Current.CancellationToken);

        items.Single().IsStalePending.Should().BeFalse(
            "only Pending status triggers the stale flag, not Failed");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class InboxTestDbContext : DbContext, IAppDbContext
    {
        private InboxTestDbContext(DbContextOptions<InboxTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static InboxTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<InboxTestDbContext>()
                .UseInMemoryDatabase($"darwin_inbox_tests_{Guid.NewGuid()}")
                .Options;
            return new InboxTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<ProviderCallbackInboxMessage>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.CallbackType).IsRequired();
                builder.Property(x => x.PayloadJson).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
