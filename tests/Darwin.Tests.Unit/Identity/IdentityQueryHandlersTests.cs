using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Covers admin-facing Identity query handlers:
/// <see cref="GetUsersPageHandler"/>, <see cref="GetUserOpsSummaryHandler"/>,
/// <see cref="GetUserForEditHandler"/>, <see cref="GetUserWithRolesForEditHandler"/>,
/// and <see cref="GetRolesPageHandler"/>.
/// </summary>
public sealed class IdentityQueryHandlersTests
{
    // ─── GetUsersPageHandler ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersPage_Should_ReturnAllUsers_WhenNoFilterApplied()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        db.Set<User>().AddRange(
            CreateUser("alice@darwin.test"),
            CreateUser("bob@darwin.test"),
            CreateUser("carol@darwin.test"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUsersPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 10, null, ct: TestContext.Current.CancellationToken);

        total.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetUsersPage_Should_FilterByEmail()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        db.Set<User>().AddRange(
            CreateUser("alice@darwin.test"),
            CreateUser("bob@acme.test"),
            CreateUser("carol@darwin.test"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUsersPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 10, "darwin.test", ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().OnlyContain(u => u.Email.Contains("darwin.test"));
    }

    [Fact]
    public async Task GetUsersPage_Should_FilterUnconfirmedUsers()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var confirmed = CreateUser("confirmed@darwin.test");
        confirmed.EmailConfirmed = true;
        var unconfirmed = CreateUser("unconfirmed@darwin.test");
        unconfirmed.EmailConfirmed = false;
        db.Set<User>().AddRange(confirmed, unconfirmed);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUsersPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 10, null,
            filter: Darwin.Application.Identity.DTOs.UserQueueFilter.Unconfirmed,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Email.Should().Be("unconfirmed@darwin.test");
    }

    [Fact]
    public async Task GetUsersPage_Should_FilterInactiveUsers()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var active = CreateUser("active@darwin.test");
        active.IsActive = true;
        var inactive = CreateUser("inactive@darwin.test");
        inactive.IsActive = false;
        db.Set<User>().AddRange(active, inactive);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUsersPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 10, null,
            filter: Darwin.Application.Identity.DTOs.UserQueueFilter.Inactive,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Email.Should().Be("inactive@darwin.test");
    }

    [Fact]
    public async Task GetUsersPage_Should_RespectPagination()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        for (var i = 1; i <= 5; i++)
            db.Set<User>().Add(CreateUser($"user{i:D2}@darwin.test"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUsersPageHandler(db);
        var (items, total) = await handler.HandleAsync(2, 2, null, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    // ─── GetUserOpsSummaryHandler ─────────────────────────────────────────────

    [Fact]
    public async Task GetUserOpsSummary_Should_ReturnCorrectCounts()
    {
        await using var db = IdentityQueryTestDbContext.Create();

        var active = CreateUser("active@darwin.test");
        active.IsActive = true;
        active.EmailConfirmed = true;

        var inactive = CreateUser("inactive@darwin.test");
        inactive.IsActive = false;

        var unconfirmed = CreateUser("unconfirmed@darwin.test");
        unconfirmed.EmailConfirmed = false;

        var locked = CreateUser("locked@darwin.test");
        locked.LockoutEndUtc = DateTime.UtcNow.AddHours(1);

        db.Set<User>().AddRange(active, inactive, unconfirmed, locked);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(4);
        summary.InactiveCount.Should().Be(1);
        summary.UnconfirmedCount.Should().BeGreaterThanOrEqualTo(1);
        summary.LockedCount.Should().Be(1);
    }

    // ─── GetUserForEditHandler ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserForEdit_Should_ReturnUser_WhenUserExists()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var user = CreateUser("edit@darwin.test");
        user.FirstName = "Max";
        user.LastName = "Muster";
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.FirstName.Should().Be("Max");
        result.Value.LastName.Should().Be("Muster");
    }

    [Fact]
    public async Task GetUserForEdit_Should_Fail_WhenUserNotFound()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var handler = new GetUserForEditHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task GetUserForEdit_Should_Fail_WhenUserIsSoftDeleted()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var user = CreateUser("deleted@darwin.test");
        user.IsDeleted = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    // ─── GetUserWithRolesForEditHandler ───────────────────────────────────────

    [Fact]
    public async Task GetUserWithRolesForEdit_Should_ReturnUserWithCurrentAndAllRoles()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var user = CreateUser("roleuser@darwin.test");
        var roleA = new Role("editor", "Editor", false, null);
        var roleB = new Role("viewer", "Viewer", false, null);
        db.Set<User>().Add(user);
        db.Set<Role>().AddRange(roleA, roleB);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<UserRole>().Add(new UserRole(user.Id, roleA.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserWithRolesForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.UserId.Should().Be(user.Id);
        result.Value.RoleIds.Should().ContainSingle(id => id == roleA.Id);
        result.Value.AllRoles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserWithRolesForEdit_Should_Fail_WhenUserNotFoundOrInactive()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var handler = new GetUserWithRolesForEditHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task GetUserWithRolesForEdit_Should_Fail_WhenUserIsInactive()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var user = CreateUser("inactive-roles@darwin.test");
        user.IsActive = false;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserWithRolesForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task GetUserWithRolesForEdit_Should_ExcludeDeletedRolesFromAllRoles()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var user = CreateUser("roleuser2@darwin.test");
        var activeRole = new Role("active-role", "Active", false, null);
        var deletedRole = new Role("deleted-role", "Deleted", false, null);
        deletedRole.IsDeleted = true;
        db.Set<User>().Add(user);
        db.Set<Role>().AddRange(activeRole, deletedRole);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserWithRolesForEditHandler(db, new TestLocalizer());
        var result = await handler.HandleAsync(user.Id, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.AllRoles.Should().HaveCount(1);
        result.Value.AllRoles.Single().DisplayName.Should().Be("Active");
    }

    // ─── GetRolesPageHandler ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRolesPage_Should_ReturnAllNonDeletedRoles()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        db.Set<Role>().AddRange(
            new Role("admin", "Administrator", true, "System admin"),
            new Role("editor", "Editor", false, null),
            new Role("deleted-role", "Deleted", false, null) { IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRolesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(r => r.DisplayName == "Deleted");
    }

    [Fact]
    public async Task GetRolesPage_Should_FilterBySearchTerm()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        db.Set<Role>().AddRange(
            new Role("admin", "Administrator", true, "Full admin access"),
            new Role("viewer", "Viewer", false, "Read-only access"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRolesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "admin", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().DisplayName.Should().Be("Administrator");
    }

    [Fact]
    public async Task GetRolesPage_Should_RespectPagination()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        for (var i = 1; i <= 6; i++)
            db.Set<Role>().Add(new Role($"role-{i}", $"Role {i:D2}", false, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRolesPageHandler(db);
        var (items, total) = await handler.HandleAsync(2, 2, ct: TestContext.Current.CancellationToken);

        total.Should().Be(6);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRolesPage_Should_ReturnEmptyList_WhenNoRolesExist()
    {
        await using var db = IdentityQueryTestDbContext.Create();
        var handler = new GetRolesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(string email)
    {
        return new User(email, "hashed:password", Guid.NewGuid().ToString("N"))
        {
            IsActive = true,
            EmailConfirmed = true,
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

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class IdentityQueryTestDbContext : DbContext, IAppDbContext
    {
        private IdentityQueryTestDbContext(DbContextOptions<IdentityQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static IdentityQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<IdentityQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_identity_query_tests_{Guid.NewGuid()}")
                .Options;
            return new IdentityQueryTestDbContext(options);
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

            modelBuilder.Entity<UserDevice>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.IsActive).IsRequired();
            });

            modelBuilder.Entity<Role>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Key).IsRequired();
                builder.Property(x => x.NormalizedName).IsRequired();
                builder.Property(x => x.DisplayName).IsRequired();
                builder.Ignore(x => x.UserRoles);
                builder.Ignore(x => x.RolePermissions);
            });

            modelBuilder.Entity<UserRole>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.RoleId).IsRequired();
            });
        }
    }
}
