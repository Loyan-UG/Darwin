using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Verifies that Loyalty command handlers handle null database RowVersion values safely
/// (no NullReferenceException) and honour the optional-RowVersion contract — when a client
/// supplies no RowVersion the concurrency guard is skipped, and when a non-empty RowVersion
/// is compared against a null DB value the mismatch is detected gracefully.
/// </summary>
public sealed class LoyaltyRowVersionCoverageTests
{
    private static readonly DateTime FixedUtcNow = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ─────────────────────────────────────────────────────────────────────────
    // ActivateLoyaltyAccountHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateLoyaltyAccount_Should_Succeed_WhenNullRowVersionProvided()
    {
        // Optional check: null RowVersion from client means "skip concurrency guard".
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = id,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Suspended
            // RowVersion = null — simulates legacy row without a concurrency token
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ActivateLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new ActivateLoyaltyAccountDto { Id = id, RowVersion = null },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("null RowVersion should bypass the concurrency check");
    }

    [Fact]
    public async Task ActivateLoyaltyAccount_Should_Fail_WhenDbRowVersionIsNullAndClientVersionProvided()
    {
        // DB entity has null RowVersion; client sends a non-empty token.
        // Handler normalises null → [] and detects mismatch without NullReferenceException.
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = id,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Suspended
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ActivateLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new ActivateLoyaltyAccountDto { Id = id, RowVersion = [1, 2, 3] },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("null DB RowVersion vs non-empty client version must be a concurrency conflict");
        result.Error.Should().Be("LoyaltyAccountConcurrencyConflict");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SuspendLoyaltyAccountHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SuspendLoyaltyAccount_Should_Succeed_WhenNullRowVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = id,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new SuspendLoyaltyAccountDto { Id = id, RowVersion = null },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("null RowVersion should bypass the concurrency check");
    }

    [Fact]
    public async Task SuspendLoyaltyAccount_Should_Fail_WhenDbRowVersionIsNullAndClientVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = id,
            BusinessId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SuspendLoyaltyAccountHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new SuspendLoyaltyAccountDto { Id = id, RowVersion = [4, 5, 6] },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountConcurrencyConflict");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AdjustLoyaltyPointsHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_Succeed_WhenNullRowVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 200
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AdjustLoyaltyPointsHandler(
            db,
            new FixedClock(FixedUtcNow),
            new AdjustLoyaltyPointsValidator(new TestLocalizer()),
            new TestLocalizer());

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = 50,
            RowVersion = null // optional check skipped
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("null RowVersion skips the concurrency guard");
    }

    [Fact]
    public async Task AdjustLoyaltyPoints_Should_Fail_WhenDbRowVersionIsNullAndClientVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = Guid.NewGuid(),
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 200
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AdjustLoyaltyPointsHandler(
            db,
            new FixedClock(FixedUtcNow),
            new AdjustLoyaltyPointsValidator(new TestLocalizer()),
            new TestLocalizer());

        var result = await handler.HandleAsync(new AdjustLoyaltyPointsDto
        {
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            PointsDelta = 10,
            RowVersion = [7, 8, 9] // non-empty client version vs null DB version → mismatch
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflictLoyaltyAccountModified");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateLoyaltyProgramHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLoyaltyProgram_Should_Succeed_WhenNullRowVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var programId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = businessId,
            Name = "Old Name"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramEditValidator(),
            new TestLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyProgramEditDto
        {
            Id = programId,
            BusinessId = businessId,
            Name = "New Name",
            RowVersion = null // optional check skipped
        }, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync("null RowVersion skips the concurrency guard");
    }

    [Fact]
    public async Task UpdateLoyaltyProgram_Should_ThrowValidation_WhenDbRowVersionIsNullAndClientVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var programId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = businessId,
            Name = "Test Program"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyProgramHandler(
            db,
            new LoyaltyProgramEditValidator(),
            new TestLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyProgramEditDto
        {
            Id = programId,
            BusinessId = businessId,
            Name = "New Name",
            RowVersion = [2, 4, 6] // client version vs null DB version → mismatch
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("null DB RowVersion vs non-empty client must be detected as a conflict");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoftDeleteLoyaltyProgramHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_Succeed_WhenNullRowVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var programId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Program To Delete"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new LoyaltyProgramDeleteDto { Id = programId, RowVersion = null },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("null RowVersion skips the concurrency guard");
    }

    [Fact]
    public async Task SoftDeleteLoyaltyProgram_Should_Fail_WhenDbRowVersionIsNullAndClientVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var programId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram
        {
            Id = programId,
            BusinessId = Guid.NewGuid(),
            Name = "Program"
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyProgramHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new LoyaltyProgramDeleteDto { Id = programId, RowVersion = [1, 3, 5] },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyProgramConcurrencyConflict");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateLoyaltyRewardTierHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLoyaltyRewardTier_Should_Succeed_WhenNullRowVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var programId = Guid.NewGuid();
        var tierId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = programId, BusinessId = Guid.NewGuid(), Name = "Program" });
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = programId,
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.PercentDiscount,
            RewardValue = 10m
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyRewardTierHandler(
            db,
            new LoyaltyRewardTierEditValidator(),
            new TestLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyRewardTierEditDto
        {
            Id = tierId,
            LoyaltyProgramId = programId,
            PointsRequired = 200,
            RewardType = LoyaltyRewardType.PercentDiscount,
            RewardValue = 15m,
            RowVersion = null // optional check skipped
        }, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync("null RowVersion skips the concurrency guard");
    }

    [Fact]
    public async Task UpdateLoyaltyRewardTier_Should_ThrowValidation_WhenDbRowVersionIsNullAndClientVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var programId = Guid.NewGuid();
        var tierId = Guid.NewGuid();
        db.Set<LoyaltyProgram>().Add(new LoyaltyProgram { Id = programId, BusinessId = Guid.NewGuid(), Name = "Program" });
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = programId,
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.PercentDiscount
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateLoyaltyRewardTierHandler(
            db,
            new LoyaltyRewardTierEditValidator(),
            new TestLocalizer());

        var act = () => handler.HandleAsync(new LoyaltyRewardTierEditDto
        {
            Id = tierId,
            LoyaltyProgramId = programId,
            PointsRequired = 200,
            RewardType = LoyaltyRewardType.PercentDiscount,
            RowVersion = [9, 8, 7] // client version vs null DB version → mismatch
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("null DB RowVersion vs non-empty client must be detected as a conflict");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoftDeleteLoyaltyRewardTierHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteLoyaltyRewardTier_Should_Succeed_WhenNullRowVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 100,
            RewardType = LoyaltyRewardType.FreeItem
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyRewardTierHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new LoyaltyRewardTierDeleteDto { Id = tierId, RowVersion = null },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("null RowVersion skips the concurrency guard");
    }

    [Fact]
    public async Task SoftDeleteLoyaltyRewardTier_Should_Fail_WhenDbRowVersionIsNullAndClientVersionProvided()
    {
        await using var db = LoyaltyNullRowVersionDbContext.Create();
        var tierId = Guid.NewGuid();
        db.Set<LoyaltyRewardTier>().Add(new LoyaltyRewardTier
        {
            Id = tierId,
            LoyaltyProgramId = Guid.NewGuid(),
            PointsRequired = 50,
            RewardType = LoyaltyRewardType.FreeItem
            // RowVersion = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteLoyaltyRewardTierHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(
            new LoyaltyRewardTierDeleteDto { Id = tierId, RowVersion = [2, 4, 8] },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyRewardTierConcurrencyConflict");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) { _utcNow = utcNow; }
        public DateTime UtcNow => _utcNow;
    }

    private sealed class LoyaltyNullRowVersionDbContext : DbContext, IAppDbContext
    {
        private LoyaltyNullRowVersionDbContext(DbContextOptions<LoyaltyNullRowVersionDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoyaltyNullRowVersionDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoyaltyNullRowVersionDbContext>()
                .UseInMemoryDatabase($"darwin_loyalty_null_rowversion_tests_{Guid.NewGuid()}")
                .Options;
            return new LoyaltyNullRowVersionDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            // All entities allow null RowVersion so we can test the null-DB-RowVersion path.
            modelBuilder.Entity<LoyaltyAccount>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion);
                b.Ignore(x => x.Transactions);
                b.Ignore(x => x.Redemptions);
            });

            modelBuilder.Entity<LoyaltyPointsTransaction>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<LoyaltyProgram>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired(false);
                b.Property(x => x.RowVersion);
                b.Ignore(x => x.RewardTiers);
            });

            modelBuilder.Entity<LoyaltyRewardTier>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion);
            });
        }
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
}
