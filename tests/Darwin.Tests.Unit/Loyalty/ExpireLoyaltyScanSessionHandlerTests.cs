using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.Commands;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for <see cref="ExpireLoyaltyScanSessionHandler"/> (single-session and batch paths).
/// </summary>
public sealed class ExpireLoyaltyScanSessionHandlerTests
{
    private static readonly DateTime FixedNow = new(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private ExpireLoyaltyScanSessionHandler CreateHandler(ExpireScanTestDbContext db, DateTime? utcNow = null)
        => new(db, new FakeClock(utcNow ?? FixedNow), new TestLocalizer());

    private static ScanSession BuildSession(
        Guid businessId,
        LoyaltyScanStatus status = LoyaltyScanStatus.Pending,
        DateTime? expiresAt = null,
        byte[]? rowVersion = null,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            LoyaltyAccountId = Guid.NewGuid(),
            QrCodeTokenId = Guid.NewGuid(),
            Mode = LoyaltyScanMode.Accrual,
            Status = status,
            ExpiresAtUtc = expiresAt ?? FixedNow.AddMinutes(-5), // expired by default
            Outcome = status == LoyaltyScanStatus.Pending ? "Pending" : status.ToString(),
            RowVersion = rowVersion ?? [1, 2, 3],
            IsDeleted = isDeleted
        };

    // ─── ExpireLoyaltyScanSessionHandler – single session ────────────────────

    [Fact]
    public async Task Expire_Should_Fail_WhenIdIsEmpty()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = Guid.Empty,
            BusinessId = Guid.NewGuid(),
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyScanSessionRequired");
    }

    [Fact]
    public async Task Expire_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.Empty,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyScanSessionRequired");
    }

    [Fact]
    public async Task Expire_Should_Fail_WhenSessionNotFound()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyScanSessionNotFound");
    }

    [Fact]
    public async Task Expire_Should_Fail_WhenSessionBelongsToOtherBusiness()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var session = BuildSession(businessId: Guid.NewGuid());
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = session.Id,
            BusinessId = Guid.NewGuid(), // different business
            RowVersion = session.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("session is not visible to a different business");
    }

    [Fact]
    public async Task Expire_Should_Fail_WhenSoftDeleted()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var session = BuildSession(businessId, isDeleted: true);
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = session.Id,
            BusinessId = businessId,
            RowVersion = session.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("soft-deleted sessions must not be visible");
    }

    [Fact]
    public async Task Expire_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var session = BuildSession(businessId);
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = session.Id,
            BusinessId = businessId,
            RowVersion = Array.Empty<byte>() // empty
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected as a concurrency guard");
        result.Error.Should().Be("LoyaltyScanSessionConcurrencyConflict");
    }

    [Fact]
    public async Task Expire_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var session = BuildSession(businessId, rowVersion: [1, 2, 3]);
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = session.Id,
            BusinessId = businessId,
            RowVersion = [9, 9, 9] // wrong version
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must be rejected");
        result.Error.Should().Be("LoyaltyScanSessionConcurrencyConflict");
    }

    [Fact]
    public async Task Expire_Should_Fail_WhenSessionIsNotPending()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var session = BuildSession(businessId, status: LoyaltyScanStatus.Completed);
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = session.Id,
            BusinessId = businessId,
            RowVersion = session.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("only Pending sessions can be expired");
        result.Error.Should().Be("LoyaltyScanSessionCannotExpire");
    }

    [Fact]
    public async Task Expire_Should_Fail_WhenSessionNotYetExpired()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        // session expires in the future
        var session = BuildSession(businessId,
            status: LoyaltyScanStatus.Pending,
            expiresAt: FixedNow.AddMinutes(10));
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = session.Id,
            BusinessId = businessId,
            RowVersion = session.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a session that has not yet passed its expiry cannot be expired");
        result.Error.Should().Be("LoyaltyScanSessionCannotExpire");
    }

    [Fact]
    public async Task Expire_Should_TransitionToPending_WhenSessionIsExpiredAndRowVersionMatches()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var session = BuildSession(businessId,
            status: LoyaltyScanStatus.Pending,
            expiresAt: FixedNow.AddMinutes(-1), // already past expiry
            rowVersion: [1, 2, 3]);
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = session.Id,
            BusinessId = businessId,
            RowVersion = [1, 2, 3]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<ScanSession>().AsNoTracking()
            .SingleAsync(x => x.Id == session.Id, TestContext.Current.CancellationToken);
        persisted.Status.Should().Be(LoyaltyScanStatus.Expired);
        persisted.Outcome.Should().Be("Expired");
        persisted.FailureReason.Should().Be("Expired");
    }

    [Fact]
    public async Task Expire_Should_Succeed_WhenExpiryIsExactlyNow()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        // ExpiresAtUtc == FixedNow — boundary: expiresAt <= now → can expire
        var session = BuildSession(businessId,
            status: LoyaltyScanStatus.Pending,
            expiresAt: FixedNow);
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new ExpireLoyaltyScanSessionDto
        {
            Id = session.Id,
            BusinessId = businessId,
            RowVersion = session.RowVersion
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("ExpiresAtUtc == clock.UtcNow is at the expire boundary");
    }

    // ─── ExpireLoyaltyScanSessionHandler – batch (HandleExpiredAsync) ─────────

    [Fact]
    public async Task ExpireBatch_Should_Fail_WhenBusinessIdIsEmpty()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var handler = CreateHandler(db);

        var result = await handler.HandleExpiredAsync(new ExpireExpiredLoyaltyScanSessionsDto
        {
            BusinessId = Guid.Empty
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("BusinessIdRequired");
    }

    [Fact]
    public async Task ExpireBatch_Should_Return_ZeroExpiredCount_WhenNoSessionsExist()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var handler = CreateHandler(db);

        var result = await handler.HandleExpiredAsync(new ExpireExpiredLoyaltyScanSessionsDto
        {
            BusinessId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.ExpiredCount.Should().Be(0);
    }

    [Fact]
    public async Task ExpireBatch_Should_ExpireAllEligibleSessions()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        // Add 3 expired Pending sessions
        db.Set<ScanSession>().AddRange(
            BuildSession(businessId, status: LoyaltyScanStatus.Pending, expiresAt: FixedNow.AddMinutes(-1)),
            BuildSession(businessId, status: LoyaltyScanStatus.Pending, expiresAt: FixedNow.AddMinutes(-30)),
            BuildSession(businessId, status: LoyaltyScanStatus.Pending, expiresAt: FixedNow.AddMinutes(-60))
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var result = await handler.HandleExpiredAsync(new ExpireExpiredLoyaltyScanSessionsDto
        {
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.ExpiredCount.Should().Be(3);

        var allSessions = await db.Set<ScanSession>().AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .ToListAsync(TestContext.Current.CancellationToken);
        allSessions.Should().AllSatisfy(s => s.Status.Should().Be(LoyaltyScanStatus.Expired));
    }

    [Fact]
    public async Task ExpireBatch_Should_Skip_SessionsThatAreNotYetExpired()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        // One expired, one still active
        db.Set<ScanSession>().AddRange(
            BuildSession(businessId, status: LoyaltyScanStatus.Pending, expiresAt: FixedNow.AddMinutes(-1)),
            BuildSession(businessId, status: LoyaltyScanStatus.Pending, expiresAt: FixedNow.AddMinutes(5))
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var result = await handler.HandleExpiredAsync(new ExpireExpiredLoyaltyScanSessionsDto
        {
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.ExpiredCount.Should().Be(1, "only the past-expiry session is eligible");

        var active = await db.Set<ScanSession>().AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Status == LoyaltyScanStatus.Pending)
            .ToListAsync(TestContext.Current.CancellationToken);
        active.Should().HaveCount(1, "the future-expiry session stays Pending");
    }

    [Fact]
    public async Task ExpireBatch_Should_Skip_NonPendingSessions()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        // Already Completed and Cancelled sessions should not be re-expired
        db.Set<ScanSession>().AddRange(
            BuildSession(businessId, status: LoyaltyScanStatus.Completed, expiresAt: FixedNow.AddMinutes(-1)),
            BuildSession(businessId, status: LoyaltyScanStatus.Cancelled, expiresAt: FixedNow.AddMinutes(-1))
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var result = await handler.HandleExpiredAsync(new ExpireExpiredLoyaltyScanSessionsDto
        {
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.ExpiredCount.Should().Be(0, "only Pending sessions can be expired by the batch handler");
    }

    [Fact]
    public async Task ExpireBatch_Should_Skip_SoftDeletedSessions()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<ScanSession>().Add(
            BuildSession(businessId, status: LoyaltyScanStatus.Pending, expiresAt: FixedNow.AddMinutes(-1), isDeleted: true)
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var result = await handler.HandleExpiredAsync(new ExpireExpiredLoyaltyScanSessionsDto
        {
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.ExpiredCount.Should().Be(0, "soft-deleted sessions must not be processed");
    }

    [Fact]
    public async Task ExpireBatch_Should_OnlyProcess_SessionsForTargetBusiness()
    {
        await using var db = ExpireScanTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();
        db.Set<ScanSession>().AddRange(
            BuildSession(businessId, status: LoyaltyScanStatus.Pending, expiresAt: FixedNow.AddMinutes(-1)),
            BuildSession(otherBusinessId, status: LoyaltyScanStatus.Pending, expiresAt: FixedNow.AddMinutes(-1))
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var result = await handler.HandleExpiredAsync(new ExpireExpiredLoyaltyScanSessionsDto
        {
            BusinessId = businessId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.ExpiredCount.Should().Be(1, "only the target business sessions are affected");

        var otherSession = await db.Set<ScanSession>().AsNoTracking()
            .SingleAsync(x => x.BusinessId == otherBusinessId, TestContext.Current.CancellationToken);
        otherSession.Status.Should().Be(LoyaltyScanStatus.Pending, "other business sessions must not be touched");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class ExpireScanTestDbContext : DbContext, IAppDbContext
    {
        private ExpireScanTestDbContext(DbContextOptions<ExpireScanTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ExpireScanTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ExpireScanTestDbContext>()
                .UseInMemoryDatabase($"darwin_expire_scan_tests_{Guid.NewGuid()}")
                .Options;
            return new ExpireScanTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<ScanSession>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.LoyaltyAccountId).IsRequired();
                builder.Property(x => x.QrCodeTokenId).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.Mode).IsRequired();
                builder.Property(x => x.ExpiresAtUtc).IsRequired();
                builder.Property(x => x.Outcome).IsRequired();
            });
        }
    }
}
