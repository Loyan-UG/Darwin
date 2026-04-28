using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Covers admin-side user support actions such as email confirmation and lock management.
/// </summary>
public sealed class AdminUserSupportHandlersTests
{
    [Fact]
    public async Task ConfirmUserEmailByAdmin_Should_SetEmailConfirmed()
    {
        await using var db = AdminUserSupportTestDbContext.Create();
        var user = CreateUser("support-confirm@darwin.de");
        user.EmailConfirmed = false;

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConfirmUserEmailByAdminHandler(db, new UserAdminActionValidator(), new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(new UserAdminActionDto { Id = user.Id }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        persisted.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task LockUserByAdmin_Should_SetLockout_AndRevokeRefreshTokens()
    {
        await using var db = AdminUserSupportTestDbContext.Create();
        var user = CreateUser("support-lock@darwin.de");

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtTokenService();
        var utcNow = new DateTime(2030, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var handler = new LockUserByAdminHandler(
            db,
            jwt,
            new FakeClock(utcNow),
            new FakeSecurityStampService(),
            new UserAdminActionValidator(),
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(new UserAdminActionDto { Id = user.Id }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        jwt.RevokeAllForUserCalls.Should().Be(1);
        jwt.LastRevokedUserId.Should().Be(user.Id);

        var persisted = await db.Set<User>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        persisted.LockoutEndUtc.Should().Be(utcNow.AddYears(10));
        persisted.AccessFailedCount.Should().Be(0);
        persisted.SecurityStamp.Should().Be("support-stamp");
    }

    [Fact]
    public async Task UnlockUserByAdmin_Should_ClearLockout()
    {
        await using var db = AdminUserSupportTestDbContext.Create();
        var user = CreateUser("support-unlock@darwin.de");
        user.LockoutEndUtc = new DateTime(2030, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        user.AccessFailedCount = 4;

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UnlockUserByAdminHandler(db, new UserAdminActionValidator(), new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(new UserAdminActionDto { Id = user.Id }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        persisted.LockoutEndUtc.Should().BeNull();
        persisted.AccessFailedCount.Should().Be(0);
    }

    private static User CreateUser(string email)
    {
        return new User(email, "hashed-password", "security-stamp")
        {
            FirstName = "Lena",
            LastName = "Bauer",
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

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeSecurityStampService : ISecurityStampService
    {
        public string NewStamp() => "support-stamp";

        public bool AreEqual(string? a, string? b)
            => string.Equals(a, b, StringComparison.Ordinal);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public int RevokeAllForUserCalls { get; private set; }

        public Guid? LastRevokedUserId { get; private set; }

        public Task<(string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)> IssueTokensAsync(
            Guid userId,
            string email,
            string? deviceId,
            System.Collections.Generic.IEnumerable<string>? scopes = null,
            Guid? preferredBusinessId = null,
            CancellationToken ct = default)
        {
            return Task.FromResult(("access-token", DateTime.UtcNow.AddMinutes(30), "refresh-token", DateTime.UtcNow.AddDays(7)));
        }

        public Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);

        public Task RevokeRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
        {
            RevokeAllForUserCalls++;
            LastRevokedUserId = userId;
            return Task.FromResult(1);
        }
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public System.Collections.Generic.IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class AdminUserSupportTestDbContext : DbContext, IAppDbContext
    {
        private AdminUserSupportTestDbContext(DbContextOptions<AdminUserSupportTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static AdminUserSupportTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<AdminUserSupportTestDbContext>()
                .UseInMemoryDatabase($"darwin_admin_user_support_tests_{Guid.NewGuid()}")
                .Options;

            return new AdminUserSupportTestDbContext(options);
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
            });
        }
    }
}
