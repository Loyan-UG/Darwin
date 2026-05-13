using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Loyalty;

/// <summary>
/// Unit tests for <see cref="GetRecentLoyaltyScanSessionsPageHandler"/>,
/// covering paging, filter, summary, business scoping, and soft-delete exclusion.
/// </summary>
public sealed class LoyaltyScanSessionQueryHandlerTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User MakeUser(
        string email = "test@darwin.test",
        string firstName = "Test",
        string lastName = "User",
        bool isDeleted = false) =>
        new User(email, "hashed-pw", Guid.NewGuid().ToString("N"))
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}",
            IsDeleted = isDeleted,
            RowVersion = [1]
        };

    private static LoyaltyAccount MakeAccount(Guid businessId, Guid userId, bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            UserId = userId,
            Status = LoyaltyAccountStatus.Active,
            PointsBalance = 100,
            RowVersion = [1],
            IsDeleted = isDeleted
        };

    private static ScanSession MakeSession(
        Guid businessId,
        Guid accountId,
        LoyaltyScanMode mode = LoyaltyScanMode.Accrual,
        LoyaltyScanStatus status = LoyaltyScanStatus.Completed,
        string outcome = "Accrued",
        bool isDeleted = false,
        DateTime? createdAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            LoyaltyAccountId = accountId,
            QrCodeTokenId = Guid.NewGuid(),
            Mode = mode,
            Status = status,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            Outcome = outcome,
            CreatedAtUtc = createdAt ?? DateTime.UtcNow,
            RowVersion = [1],
            IsDeleted = isDeleted
        };

    // ─── GetRecentLoyaltyScanSessionsPageHandler ──────────────────────────────

    [Fact]
    public async Task GetSessions_Should_ReturnEmpty_WhenNoSessions()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);

        var (items, total) = await handler.HandleAsync(Guid.NewGuid(), ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetSessions_Should_ExcludeSoftDeletedSessions()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser();
        var account = MakeAccount(businessId, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().Add(MakeSession(businessId, account.Id, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty("soft-deleted sessions must not be visible");
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetSessions_Should_ExcludeSessionsForOtherBusiness()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBiz = Guid.NewGuid();
        var user = MakeUser();
        var account = MakeAccount(otherBiz, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().Add(MakeSession(otherBiz, account.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty("sessions from a different business must not appear");
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetSessions_Should_ExcludeSessionsWithSoftDeletedAccount()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser();
        var deletedAccount = MakeAccount(businessId, user.Id, isDeleted: true);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(deletedAccount);
        db.Set<ScanSession>().Add(MakeSession(businessId, deletedAccount.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty("sessions linked to a soft-deleted account must not be visible");
    }

    [Fact]
    public async Task GetSessions_Should_ExcludeSessionsWithSoftDeletedUser()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var deletedUser = MakeUser(isDeleted: true);
        var account = MakeAccount(businessId, deletedUser.Id);
        db.Set<User>().Add(deletedUser);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().Add(MakeSession(businessId, account.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty("sessions linked to a soft-deleted user must not be visible");
    }

    [Fact]
    public async Task GetSessions_Should_ReturnCorrectProjection()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser("ada@darwin.test", "Ada", "Lovelace");
        var account = MakeAccount(businessId, user.Id);
        var session = MakeSession(businessId, account.Id,
            mode: LoyaltyScanMode.Redemption,
            status: LoyaltyScanStatus.Completed,
            outcome: "Redeemed");
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        var item = items.Single();
        item.Id.Should().Be(session.Id);
        item.BusinessId.Should().Be(businessId);
        item.LoyaltyAccountId.Should().Be(account.Id);
        item.CustomerEmail.Should().Be("ada@darwin.test");
        item.CustomerDisplayName.Should().Be("Ada Lovelace");
        item.Mode.Should().Be(LoyaltyScanMode.Redemption);
        item.Status.Should().Be(LoyaltyScanStatus.Completed);
        item.Outcome.Should().Be("Redeemed");
    }

    [Fact]
    public async Task GetSessions_Should_FallbackToEmail_WhenNameIsEmpty()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser("noname@darwin.test", firstName: "", lastName: "");
        var account = MakeAccount(businessId, user.Id);
        var session = MakeSession(businessId, account.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, _) = await handler.HandleAsync(businessId, ct: TestContext.Current.CancellationToken);

        items.Single().CustomerDisplayName.Should().Be("noname@darwin.test",
            "when both names are empty the email is used as display name");
    }

    [Fact]
    public async Task GetSessions_Should_Filter_ByMode()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser();
        var account = MakeAccount(businessId, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().AddRange(
            MakeSession(businessId, account.Id, mode: LoyaltyScanMode.Accrual),
            MakeSession(businessId, account.Id, mode: LoyaltyScanMode.Redemption)
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, mode: LoyaltyScanMode.Accrual,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Mode.Should().Be(LoyaltyScanMode.Accrual);
    }

    [Fact]
    public async Task GetSessions_Should_Filter_ByStatus()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser();
        var account = MakeAccount(businessId, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().AddRange(
            MakeSession(businessId, account.Id, status: LoyaltyScanStatus.Completed, outcome: "Accrued"),
            MakeSession(businessId, account.Id, status: LoyaltyScanStatus.Expired, outcome: "Expired"),
            MakeSession(businessId, account.Id, status: LoyaltyScanStatus.Pending, outcome: "Pending")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, status: LoyaltyScanStatus.Expired,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Status.Should().Be(LoyaltyScanStatus.Expired);
    }

    [Fact]
    public async Task GetSessions_Should_NormalizePage_WhenPageIsLessThanOne()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser();
        var account = MakeAccount(businessId, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().Add(MakeSession(businessId, account.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, page: -5, pageSize: 10,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "page is clamped to 1");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSessions_Should_ClampPageSize_ToMaximumOf200()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser();
        var account = MakeAccount(businessId, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().Add(MakeSession(businessId, account.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        // This should not throw; large pageSize is silently clamped to 200
        var (items, total) = await handler.HandleAsync(businessId, page: 1, pageSize: 9999,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().HaveCount(1);
    }

    // ─── GetSummaryAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_Should_ReturnZeroBaseline_WhenNoSessionsExist()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);

        var summary = await handler.GetSummaryAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.AccrualCount.Should().Be(0);
        summary.RedemptionCount.Should().Be(0);
        summary.PendingCount.Should().Be(0);
        summary.ExpiredCount.Should().Be(0);
        summary.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSummary_Should_CountCorrectly_ForMixedSessions()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser();
        var account = MakeAccount(businessId, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().AddRange(
            MakeSession(businessId, account.Id, mode: LoyaltyScanMode.Accrual, status: LoyaltyScanStatus.Completed, outcome: "Accrued"),
            MakeSession(businessId, account.Id, mode: LoyaltyScanMode.Accrual, status: LoyaltyScanStatus.Expired, outcome: "Expired"),
            MakeSession(businessId, account.Id, mode: LoyaltyScanMode.Redemption, status: LoyaltyScanStatus.Completed, outcome: "Redeemed"),
            MakeSession(businessId, account.Id, mode: LoyaltyScanMode.Accrual, status: LoyaltyScanStatus.Pending, outcome: "Pending")
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var summary = await handler.GetSummaryAsync(businessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(4);
        summary.AccrualCount.Should().Be(3);
        summary.RedemptionCount.Should().Be(1);
        summary.PendingCount.Should().Be(1);
        summary.ExpiredCount.Should().Be(1);
        summary.FailureCount.Should().BeGreaterThanOrEqualTo(1, "Expired counts as failure");
    }

    [Fact]
    public async Task GetSummary_Should_ExcludeSoftDeletedSessions()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var user = MakeUser();
        var account = MakeAccount(businessId, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().Add(account);
        db.Set<ScanSession>().AddRange(
            MakeSession(businessId, account.Id, isDeleted: true),
            MakeSession(businessId, account.Id)
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var summary = await handler.GetSummaryAsync(businessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(1, "soft-deleted sessions must be excluded from the summary");
    }

    [Fact]
    public async Task GetSummary_Should_OnlyCount_SessionsForTargetBusiness()
    {
        await using var db = ScanSessionQueryDbContext.Create();
        var businessId = Guid.NewGuid();
        var otherBiz = Guid.NewGuid();
        var user = MakeUser();
        var account1 = MakeAccount(businessId, user.Id);
        var account2 = MakeAccount(otherBiz, user.Id);
        db.Set<User>().Add(user);
        db.Set<LoyaltyAccount>().AddRange(account1, account2);
        db.Set<ScanSession>().AddRange(
            MakeSession(businessId, account1.Id),
            MakeSession(businessId, account1.Id),
            MakeSession(otherBiz, account2.Id)
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRecentLoyaltyScanSessionsPageHandler(db);
        var summary = await handler.GetSummaryAsync(businessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(2, "only the target business sessions count");
    }

    // ─── Test DbContext ───────────────────────────────────────────────────────

    private sealed class ScanSessionQueryDbContext : DbContext, IAppDbContext
    {
        private ScanSessionQueryDbContext(DbContextOptions<ScanSessionQueryDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ScanSessionQueryDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ScanSessionQueryDbContext>()
                .UseInMemoryDatabase($"darwin_scan_query_tests_{Guid.NewGuid()}")
                .Options;
            return new ScanSessionQueryDbContext(options);
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
                builder.Property(x => x.Outcome).IsRequired();
            });

            modelBuilder.Entity<LoyaltyAccount>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Transactions);
                builder.Ignore(x => x.Redemptions);
            });

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
        }
    }
}
