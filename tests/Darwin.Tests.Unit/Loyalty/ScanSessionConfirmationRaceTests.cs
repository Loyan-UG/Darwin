using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Services;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

public sealed class ScanSessionConfirmationRaceTests
{
    private static readonly DateTime FixedUtcNow = new(2030, 1, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid StaffUserId = Guid.Parse("1f7e90a8-4f3f-4f5f-a2d4-9c9f6b2d58f7");

    [Fact]
    public async Task ConfirmAccrual_Should_SucceedOnce_And_BlockConcurrentConfirmation_WithPostSaveConcurrencyConflict()
    {
        var databaseName = $"darwin_loyalty_accrual_race_{Guid.NewGuid():N}";
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var tokenValue = "scan-token-accrual-race";

        await using (var seedDb = ScanSessionRaceTestDbContext.Create(databaseName))
        {
            SeedSessionGraph(
                seedDb,
                businessId,
                accountId,
                userId,
                tokenValue,
                mode: LoyaltyScanMode.Accrual,
                pointsBalance: 50,
                lifetimePoints: 120,
                selectedRewardsJson: null);

            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var leaderDb = ScanSessionRaceTestDbContext.Create(databaseName, ScanSessionRaceMode.Leader, 120);
        await using var followerDb = ScanSessionRaceTestDbContext.Create(databaseName, ScanSessionRaceMode.Follower);

        var localizer = new TestStringLocalizer<ValidationResource>();

        var leaderHandler = CreateConfirmAccrualHandler(leaderDb, localizer);
        var followerHandler = CreateConfirmAccrualHandler(followerDb, localizer);

        var dto = new ConfirmAccrualFromSessionDto
        {
            ScanSessionToken = tokenValue,
            Points = 4,
            Note = "Race test note"
        };

        var leaderTask = HandleResultSafely(() =>
            leaderHandler.HandleAsync(dto, businessId, TestContext.Current.CancellationToken));

        var followerTask = Task.Run(async () =>
        {
            await Task.Delay(30, TestContext.Current.CancellationToken);
            return await HandleResultSafely(() =>
                followerHandler.HandleAsync(dto, businessId, TestContext.Current.CancellationToken));
        });

        await Task.WhenAll(leaderTask, followerTask);

        var results = new[] { await leaderTask, await followerTask };

        var successCount = results.Count(x => x.IsSuccess);
        var failure = results.Single(x => !x.IsSuccess);

        successCount.Should().Be(1);
        failure.ErrorMessage.Should().Be("LoyaltyScanSessionConcurrencyConflict");

        await using var verifyDb = ScanSessionRaceTestDbContext.Create(databaseName);
        var transactionCount = await verifyDb.Set<LoyaltyPointsTransaction>()
            .CountAsync(TestContext.Current.CancellationToken);
        var account = await verifyDb.Set<LoyaltyAccount>()
            .SingleAsync(x => x.Id == accountId, TestContext.Current.CancellationToken);

        transactionCount.Should().Be(1);
        account.PointsBalance.Should().Be(54);
    }

    [Fact]
    public async Task ConfirmRedemption_Should_SucceedOnce_And_BlockConcurrentConfirmation_WithPostSaveConcurrencyConflict()
    {
        var databaseName = $"darwin_loyalty_redemption_race_{Guid.NewGuid():N}";
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var tokenValue = "scan-token-redemption-race";

        var reward = new SelectedRewardItemDto
        {
            LoyaltyRewardTierId = Guid.NewGuid(),
            Quantity = 2,
            RequiredPointsPerUnit = 12
        };

        await using (var seedDb = ScanSessionRaceTestDbContext.Create(databaseName))
        {
            SeedSessionGraph(
                seedDb,
                businessId,
                accountId,
                userId,
                tokenValue,
                mode: LoyaltyScanMode.Redemption,
                pointsBalance: 100,
                lifetimePoints: 180,
                selectedRewardsJson: JsonSerializer.Serialize(new[] { reward }));

            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var leaderDb = ScanSessionRaceTestDbContext.Create(databaseName, ScanSessionRaceMode.Leader, 120);
        await using var followerDb = ScanSessionRaceTestDbContext.Create(databaseName, ScanSessionRaceMode.Follower);

        var localizer = new TestStringLocalizer<ValidationResource>();

        var leaderHandler = CreateConfirmRedemptionHandler(leaderDb, localizer);
        var followerHandler = CreateConfirmRedemptionHandler(followerDb, localizer);

        var dto = new ConfirmRedemptionFromSessionDto
        {
            ScanSessionToken = tokenValue
        };

        var leaderTask = HandleResultSafely(() =>
            leaderHandler.HandleAsync(dto, businessId, TestContext.Current.CancellationToken));

        var followerTask = Task.Run(async () =>
        {
            await Task.Delay(30, TestContext.Current.CancellationToken);
            return await HandleResultSafely(() =>
                followerHandler.HandleAsync(dto, businessId, TestContext.Current.CancellationToken));
        });

        await Task.WhenAll(leaderTask, followerTask);

        var results = new[] { await leaderTask, await followerTask };

        var successCount = results.Count(x => x.IsSuccess);
        var failure = results.Single(x => !x.IsSuccess);

        successCount.Should().Be(1);
        failure.ErrorMessage.Should().Be("LoyaltyScanSessionConcurrencyConflict");

        await using var verifyDb = ScanSessionRaceTestDbContext.Create(databaseName);
        var transactionCount = await verifyDb.Set<LoyaltyPointsTransaction>()
            .CountAsync(TestContext.Current.CancellationToken);
        var redemptionCount = await verifyDb.Set<LoyaltyRewardRedemption>()
            .CountAsync(TestContext.Current.CancellationToken);
        var account = await verifyDb.Set<LoyaltyAccount>()
            .SingleAsync(x => x.Id == accountId, TestContext.Current.CancellationToken);

        transactionCount.Should().Be(1);
        redemptionCount.Should().Be(1);
        account.PointsBalance.Should().Be(76); // 100 - (2 * 12)
    }

    private static ConfirmAccrualFromSessionHandler CreateConfirmAccrualHandler(
        ScanSessionRaceTestDbContext db,
        IStringLocalizer<ValidationResource> localizer)
    {
        return new ConfirmAccrualFromSessionHandler(
            db,
            new StubCurrentUserService(StaffUserId),
            new StubClock(FixedUtcNow),
            CreateResolver(db, localizer),
            localizer);
    }

    private static ConfirmRedemptionFromSessionHandler CreateConfirmRedemptionHandler(
        ScanSessionRaceTestDbContext db,
        IStringLocalizer<ValidationResource> localizer)
    {
        return new ConfirmRedemptionFromSessionHandler(
            db,
            new StubCurrentUserService(StaffUserId),
            new StubClock(FixedUtcNow),
            CreateResolver(db, localizer),
            localizer);
    }

    private static ScanSessionTokenResolver CreateResolver(
        ScanSessionRaceTestDbContext db,
        IStringLocalizer<ValidationResource> localizer)
    {
        return new ScanSessionTokenResolver(db, new StubClock(FixedUtcNow), localizer);
    }

    private static void SeedSessionGraph(
        ScanSessionRaceTestDbContext db,
        Guid businessId,
        Guid accountId,
        Guid userId,
        string tokenValue,
        LoyaltyScanMode mode,
        int pointsBalance,
        int lifetimePoints,
        string? selectedRewardsJson)
    {
        var tokenId = Guid.NewGuid();

        db.Set<User>().Add(CreateUser(userId, "test.user@darwin.test", "Test", "User"));
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = pointsBalance,
            LifetimePoints = lifetimePoints,
            RowVersion = [1, 2, 3]
        });

        db.Set<QrCodeToken>().Add(new QrCodeToken
        {
            Id = tokenId,
            Token = tokenValue,
            UserId = userId,
            LoyaltyAccountId = accountId,
            Purpose = mode == LoyaltyScanMode.Redemption ? QrTokenPurpose.Redemption : QrTokenPurpose.Accrual,
            IssuedAtUtc = FixedUtcNow.AddMinutes(-1),
            ExpiresAtUtc = FixedUtcNow.AddMinutes(5),
            RowVersion = [1, 2, 3]
        });

        db.Set<ScanSession>().Add(new ScanSession
        {
            Id = Guid.NewGuid(),
            QrCodeTokenId = tokenId,
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            Mode = mode,
            Status = LoyaltyScanStatus.Pending,
            SelectedRewardsJson = selectedRewardsJson,
            ExpiresAtUtc = FixedUtcNow.AddMinutes(5),
            Outcome = "Pending",
            RowVersion = [1, 2, 3]
        });
    }

    private static async Task<(bool IsSuccess, string? ErrorMessage)> HandleResultSafely<T>(
        Func<Task<Darwin.Shared.Results.Result<T>>> action)
    {
        var result = await action().ConfigureAwait(false);
        return (result.Succeeded, result.Error);
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;

        public StubCurrentUserService(Guid userId) => _userId = userId;

        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class StubClock : IClock
    {
        public StubClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private static User CreateUser(Guid userId, string email, string firstName, string lastName)
    {
        return new User(email, "hashed-password", "stamp")
        {
            Id = userId,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            IsActive = true,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}",
            RowVersion = [1, 2, 3]
        };
    }

    private enum ScanSessionRaceMode
    {
        Standard,
        Leader,
        Follower
    }

    private sealed class ScanSessionRaceTestDbContext : DbContext, IAppDbContext
    {
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> RaceSaveSignals = new();
        private readonly ScanSessionRaceMode _raceMode;
        private readonly string _databaseName;
        private readonly int? _leaderDelayMs;

        private ScanSessionRaceTestDbContext(
            DbContextOptions<ScanSessionRaceTestDbContext> options,
            ScanSessionRaceMode raceMode,
            string databaseName,
            int? leaderDelayMs = null) : base(options)
        {
            _raceMode = raceMode;
            _databaseName = databaseName;
            _leaderDelayMs = leaderDelayMs;
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ScanSessionRaceTestDbContext Create(
            string databaseName,
            ScanSessionRaceMode raceMode = ScanSessionRaceMode.Standard,
            int? leaderDelayMs = null)
        {
            var options = new DbContextOptionsBuilder<ScanSessionRaceTestDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ScanSessionRaceTestDbContext(options, raceMode, databaseName, leaderDelayMs);
        }

        public static ScanSessionRaceTestDbContext Create()
        {
            return Create($"darwin_loyalty_confirmation_tests_{Guid.NewGuid():N}");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var signal = RaceSaveSignals.GetOrAdd(_databaseName, static _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

            if (_raceMode == ScanSessionRaceMode.Follower)
            {
                await signal.Task.WaitAsync(ct).ConfigureAwait(false);
                throw new DbUpdateConcurrencyException("LoyaltyScanSessionConcurrencyConflict");
            }

            if (_leaderDelayMs.HasValue)
            {
                await Task.Delay(_leaderDelayMs.Value, ct).ConfigureAwait(false);
            }

            var result = await base.SaveChangesAsync(ct).ConfigureAwait(false);

            if (_raceMode == ScanSessionRaceMode.Leader)
            {
                signal.TrySetResult(true);
            }

            return result;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.NormalizedEmail).IsRequired();
                builder.Property(x => x.UserName).IsRequired();
                builder.Property(x => x.NormalizedUserName).IsRequired();
                builder.Property(x => x.PasswordHash).IsRequired();
                builder.Property(x => x.SecurityStamp).IsRequired();
                builder.Property(x => x.Locale).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.Timezone).IsRequired();
                builder.Property(x => x.ChannelsOptInJson).IsRequired();
                builder.Property(x => x.FirstTouchUtmJson).IsRequired();
                builder.Property(x => x.LastTouchUtmJson).IsRequired();
                builder.Property(x => x.ExternalIdsJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.Logins);
                builder.Ignore(x => x.Tokens);
                builder.Ignore(x => x.TwoFactorSecrets);
                builder.Ignore(x => x.Devices);
                builder.Ignore(x => x.BusinessFavorites);
                builder.Ignore(x => x.BusinessLikes);
                builder.Ignore(x => x.BusinessReviews);
                builder.Ignore(x => x.EngagementSnapshot);
            });

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Transactions);
                builder.Ignore(x => x.Redemptions);
            });

            modelBuilder.Entity<QrCodeToken>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Property(x => x.Token).IsRequired();
                builder.Property(x => x.UserId).IsRequired();
            });

            modelBuilder.Entity<ScanSession>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.QrCodeTokenId).IsRequired();
                builder.Property(x => x.LoyaltyAccountId).IsRequired();
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.Mode).IsRequired();
                builder.Property(x => x.ExpiresAtUtc).IsRequired();
                builder.Property(x => x.Outcome).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<LoyaltyRewardRedemption>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.LoyaltyAccountId).IsRequired();
                builder.Property(x => x.LoyaltyRewardTierId).IsRequired();
                builder.Property(x => x.Status).IsRequired();
            });

            modelBuilder.Entity<LoyaltyPointsTransaction>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Property(x => x.LoyaltyAccountId).IsRequired();
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.Type).IsRequired();
            });
        }
    }
}
