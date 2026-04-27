using System;
using System.Text.Json;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for <see cref="MarkInactiveReminderAttemptHandler"/>
/// and <see cref="MarkInactiveReminderSentHandler"/>.
/// </summary>
public sealed class MarkInactiveReminderHandlersTests
{
    private static readonly DateTime FixedNow = new(2030, 3, 10, 8, 0, 0, DateTimeKind.Utc);

    // ─── MarkInactiveReminderAttemptHandler ───────────────────────────────────

    [Fact]
    public async Task MarkAttempt_Should_Fail_WhenRequestIsNull()
    {
        await using var db = ReminderTestDbContext.Create();
        var handler = CreateAttemptHandler(db);

        var result = await handler.HandleAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RequestPayloadRequired");
    }

    [Fact]
    public async Task MarkAttempt_Should_Fail_WhenUserIdIsEmpty()
    {
        await using var db = ReminderTestDbContext.Create();
        var handler = CreateAttemptHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderAttemptDto
        {
            UserId = Guid.Empty,
            Outcome = "Sent"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserIdRequired");
    }

    [Fact]
    public async Task MarkAttempt_Should_Fail_WhenOutcomeIsInvalid()
    {
        await using var db = ReminderTestDbContext.Create();
        var handler = CreateAttemptHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderAttemptDto
        {
            UserId = Guid.NewGuid(),
            Outcome = "Unknown"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InactiveReminderOutcomeInvalid");
    }

    [Fact]
    public async Task MarkAttempt_Should_Fail_WhenSnapshotNotFound()
    {
        await using var db = ReminderTestDbContext.Create();
        var handler = CreateAttemptHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderAttemptDto
        {
            UserId = Guid.NewGuid(),
            Outcome = "Sent"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserEngagementSnapshotNotFound");
    }

    [Fact]
    public async Task MarkAttempt_WithSentOutcome_Should_IncrementSentCount()
    {
        await using var db = ReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            SnapshotJson = "{}",
            CalculatedAtUtc = FixedNow.AddHours(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAttemptHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderAttemptDto
        {
            UserId = userId,
            Outcome = "Sent"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var snapshot = await db.Set<UserEngagementSnapshot>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        var meta = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(snapshot.SnapshotJson)!;
        meta[ReminderMetadataKeys.InactiveReminderSentCount].GetInt64().Should().Be(1);
        meta[ReminderMetadataKeys.LastInactiveReminderOutcome].GetString().Should().Be("Sent");
    }

    [Fact]
    public async Task MarkAttempt_WithFailedOutcome_Should_IncrementFailedCount()
    {
        await using var db = ReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            SnapshotJson = "{}",
            CalculatedAtUtc = FixedNow.AddHours(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAttemptHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderAttemptDto
        {
            UserId = userId,
            Outcome = "Failed",
            OutcomeCode = "Gateway.Timeout"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var snapshot = await db.Set<UserEngagementSnapshot>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        var meta = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(snapshot.SnapshotJson)!;
        meta[ReminderMetadataKeys.InactiveReminderFailedCount].GetInt64().Should().Be(1);
        meta[ReminderMetadataKeys.LastInactiveReminderOutcome].GetString().Should().Be("Failed");
        meta[ReminderMetadataKeys.LastInactiveReminderOutcomeCode].GetString().Should().Be("Gateway.Timeout");
    }

    [Fact]
    public async Task MarkAttempt_WithSuppressedOutcome_Should_IncrementSuppressedCount()
    {
        await using var db = ReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            SnapshotJson = "{}",
            CalculatedAtUtc = FixedNow.AddHours(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAttemptHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderAttemptDto
        {
            UserId = userId,
            Outcome = "Suppressed",
            OutcomeCode = "CooldownActive"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var snapshot = await db.Set<UserEngagementSnapshot>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        var meta = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(snapshot.SnapshotJson)!;
        meta[ReminderMetadataKeys.InactiveReminderSuppressedCount].GetInt64().Should().Be(1);
        meta[ReminderMetadataKeys.LastInactiveReminderOutcome].GetString().Should().Be("Suppressed");
    }

    [Fact]
    public async Task MarkAttempt_WithSentOutcome_Should_AccumulateCounts_OnRepeatedCalls()
    {
        await using var db = ReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            SnapshotJson = "{}",
            CalculatedAtUtc = FixedNow.AddHours(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateAttemptHandler(db);

        await handler.HandleAsync(new MarkInactiveReminderAttemptDto { UserId = userId, Outcome = "Sent" }, TestContext.Current.CancellationToken);
        await handler.HandleAsync(new MarkInactiveReminderAttemptDto { UserId = userId, Outcome = "Sent" }, TestContext.Current.CancellationToken);
        await handler.HandleAsync(new MarkInactiveReminderAttemptDto { UserId = userId, Outcome = "Sent" }, TestContext.Current.CancellationToken);

        var snapshot = await db.Set<UserEngagementSnapshot>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        var meta = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(snapshot.SnapshotJson)!;
        meta[ReminderMetadataKeys.InactiveReminderSentCount].GetInt64().Should().Be(3);
    }

    // ─── MarkInactiveReminderSentHandler ──────────────────────────────────────

    [Fact]
    public async Task MarkSent_Should_Fail_WhenRequestIsNull()
    {
        await using var db = ReminderTestDbContext.Create();
        var handler = CreateSentHandler(db);

        var result = await handler.HandleAsync(null!, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RequestPayloadRequired");
    }

    [Fact]
    public async Task MarkSent_Should_Fail_WhenUserIdIsEmpty()
    {
        await using var db = ReminderTestDbContext.Create();
        var handler = CreateSentHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderSentDto
        {
            UserId = Guid.Empty
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserIdRequired");
    }

    [Fact]
    public async Task MarkSent_Should_Fail_WhenSnapshotNotFound()
    {
        await using var db = ReminderTestDbContext.Create();
        var handler = CreateSentHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderSentDto
        {
            UserId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserEngagementSnapshotNotFound");
    }

    [Fact]
    public async Task MarkSent_Should_IncrementSentCount_AndSetTimestamp()
    {
        await using var db = ReminderTestDbContext.Create();
        var userId = Guid.NewGuid();
        var sentAt = new DateTime(2030, 3, 10, 7, 30, 0, DateTimeKind.Utc);
        db.Set<UserEngagementSnapshot>().Add(new UserEngagementSnapshot
        {
            UserId = userId,
            SnapshotJson = "{}",
            CalculatedAtUtc = FixedNow.AddHours(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateSentHandler(db);

        var result = await handler.HandleAsync(new MarkInactiveReminderSentDto
        {
            UserId = userId,
            SentAtUtc = sentAt
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var snapshot = await db.Set<UserEngagementSnapshot>().AsNoTracking()
            .SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
        var meta = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>(snapshot.SnapshotJson)!;
        meta[ReminderMetadataKeys.InactiveReminderSentCount].GetInt64().Should().Be(1);
        meta.ContainsKey(ReminderMetadataKeys.LastInactiveReminderSentAtUtc).Should().BeTrue();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static MarkInactiveReminderAttemptHandler CreateAttemptHandler(IAppDbContext db)
        => new(db, new FakeClock(FixedNow), new TestLocalizer());

    private static MarkInactiveReminderSentHandler CreateSentHandler(IAppDbContext db)
        => new(db, new FakeClock(FixedNow), new TestLocalizer());

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

    private sealed class ReminderTestDbContext : DbContext, IAppDbContext
    {
        private ReminderTestDbContext(DbContextOptions<ReminderTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ReminderTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ReminderTestDbContext>()
                .UseInMemoryDatabase($"darwin_reminder_handlers_tests_{Guid.NewGuid()}")
                .Options;
            return new ReminderTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<UserEngagementSnapshot>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.SnapshotJson).IsRequired();
                builder.Ignore(x => x.User);
            });
        }
    }
}
