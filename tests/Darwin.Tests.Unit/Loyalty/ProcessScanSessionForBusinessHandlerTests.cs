using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Application.Loyalty.Services;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Loyalty;

public sealed class ProcessScanSessionForBusinessHandlerTests
{
    private static readonly DateTime FixedUtcNow = new(2030, 1, 5, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_Should_ReturnBusinessView_WithFriendlyDisplayName_AndSelectedRewards()
    {
        await using var db = ProcessScanSessionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var user = CreateUser("mila.wagner@darwin.test", firstName: "Mila", lastName: "Wagner");
        var rewardTierId = Guid.NewGuid();

        SeedSessionGraph(
            db,
            tokenValue: "scan-token-1",
            businessId: businessId,
            accountId: accountId,
            user: user,
            selectedRewardsJson: JsonSerializer.Serialize(new[]
            {
                new SelectedRewardItemDto
                {
                    LoyaltyRewardTierId = rewardTierId,
                    Quantity = 2,
                    RequiredPointsPerUnit = 120
                }
            }));

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync("scan-token-1", businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.LoyaltyAccountId.Should().Be(accountId);
        result.Value.CurrentPointsBalance.Should().Be(180);
        result.Value.CustomerDisplayName.Should().Be("Mila W.");
        result.Value.SelectedRewards.Should().ContainSingle();
        result.Value.SelectedRewards[0].LoyaltyRewardTierId.Should().Be(rewardTierId);
        result.Value.SelectedRewards[0].Quantity.Should().Be(2);
        result.Value.SelectedRewards[0].RequiredPointsPerUnit.Should().Be(120);
    }

    [Fact]
    public async Task HandleAsync_Should_FallBackToMaskedEmail_AndIgnoreMalformedRewardsJson()
    {
        await using var db = ProcessScanSessionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = CreateUser("customer@example.com");

        SeedSessionGraph(
            db,
            tokenValue: "scan-token-2",
            businessId: businessId,
            accountId: Guid.NewGuid(),
            user: user,
            selectedRewardsJson: "{ malformed json");

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync("scan-token-2", businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CustomerDisplayName.Should().Be("c***@example.com");
        result.Value.SelectedRewards.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_WhenResolverCannotResolveToken()
    {
        await using var db = ProcessScanSessionTestDbContext.Create();
        var handler = CreateHandler(db);

        var result = await handler.HandleAsync("missing-token", Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ScanSessionTokenNotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_WhenLoyaltyAccountCannotBeLoaded()
    {
        await using var db = ProcessScanSessionTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = CreateUser("missing-account@darwin.test", firstName: "Sara");
        var missingAccountId = Guid.NewGuid();

        var tokenId = Guid.NewGuid();

        db.Set<User>().Add(user);
        db.Set<QrCodeToken>().Add(new QrCodeToken
        {
            Id = tokenId,
            Token = "scan-token-3",
            UserId = user.Id,
            LoyaltyAccountId = missingAccountId,
            Purpose = QrTokenPurpose.Redemption,
            IssuedAtUtc = FixedUtcNow.AddMinutes(-1),
            ExpiresAtUtc = FixedUtcNow.AddMinutes(5),
            RowVersion = [1]
        });
        db.Set<ScanSession>().Add(new ScanSession
        {
            Id = Guid.NewGuid(),
            QrCodeTokenId = tokenId,
            LoyaltyAccountId = missingAccountId,
            BusinessId = businessId,
            Mode = LoyaltyScanMode.Redemption,
            Status = LoyaltyScanStatus.Pending,
            ExpiresAtUtc = FixedUtcNow.AddMinutes(2),
            Outcome = "Pending",
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync("scan-token-3", businessId, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("LoyaltyAccountNotFoundForScanSession");
    }

    private static ProcessScanSessionForBusinessHandler CreateHandler(ProcessScanSessionTestDbContext db)
    {
        var localizer = new TestStringLocalizer<ValidationResource>();
        var resolver = new ScanSessionTokenResolver(db, new StubClock(FixedUtcNow), localizer);
        return new ProcessScanSessionForBusinessHandler(
            db,
            new StubCurrentUserService(Guid.NewGuid()),
            new StubClock(FixedUtcNow),
            resolver,
            localizer);
    }

    private static void SeedSessionGraph(
        ProcessScanSessionTestDbContext db,
        string tokenValue,
        Guid businessId,
        Guid accountId,
        User user,
        string? selectedRewardsJson)
    {
        var tokenId = Guid.NewGuid();

        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(new LoyaltyAccount
        {
            Id = accountId,
            BusinessId = businessId,
            UserId = user.Id,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 180,
            LifetimePoints = 420,
            RowVersion = [1]
        });
        db.Set<QrCodeToken>().Add(new QrCodeToken
        {
            Id = tokenId,
            Token = tokenValue,
            UserId = user.Id,
            LoyaltyAccountId = accountId,
            Purpose = QrTokenPurpose.Redemption,
            IssuedAtUtc = FixedUtcNow.AddMinutes(-1),
            ExpiresAtUtc = FixedUtcNow.AddMinutes(5),
            RowVersion = [1]
        });
        db.Set<ScanSession>().Add(new ScanSession
        {
            Id = Guid.NewGuid(),
            QrCodeTokenId = tokenId,
            LoyaltyAccountId = accountId,
            BusinessId = businessId,
            Mode = LoyaltyScanMode.Redemption,
            Status = LoyaltyScanStatus.Pending,
            SelectedRewardsJson = selectedRewardsJson,
            ExpiresAtUtc = FixedUtcNow.AddMinutes(2),
            Outcome = "Pending",
            RowVersion = [1]
        });
        db.SaveChanges();
    }

    private static User CreateUser(string email, string? firstName = null, string? lastName = null)
    {
        return new User(email, "hashed-password", "stamp")
        {
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

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;

        public StubCurrentUserService(Guid userId)
        {
            _userId = userId;
        }

        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class StubClock : IClock
    {
        public StubClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class ProcessScanSessionTestDbContext : DbContext, IAppDbContext
    {
        private ProcessScanSessionTestDbContext(DbContextOptions<ProcessScanSessionTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ProcessScanSessionTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ProcessScanSessionTestDbContext>()
                .UseInMemoryDatabase($"darwin_process_scan_session_tests_{Guid.NewGuid()}")
                .Options;
            return new ProcessScanSessionTestDbContext(options);
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
                builder.Property(x => x.Token).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<ScanSession>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.QrCodeTokenId).IsRequired();
                builder.Property(x => x.LoyaltyAccountId).IsRequired();
                builder.Property(x => x.BusinessId).IsRequired();
                builder.Property(x => x.Outcome).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
