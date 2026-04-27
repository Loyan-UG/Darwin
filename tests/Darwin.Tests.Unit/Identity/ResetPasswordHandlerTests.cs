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
/// Unit tests for <see cref="ResetPasswordHandler"/>.
/// </summary>
public sealed class ResetPasswordHandlerTests
{
    private static readonly DateTime FixedNow = new(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ResetPassword_Should_UpdateHash_AndMarkToken_WhenTokenIsValid()
    {
        await using var db = ResetPasswordTestDbContext.Create();
        var user = CreateUser("reset-me@darwin.de");
        db.Set<User>().Add(user);

        var token = new UserToken(user.Id, "PasswordReset", "valid-token", FixedNow.AddHours(1));
        db.Set<UserToken>().Add(token);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var hasher = new FakeHasher();
        var stamps = new FakeStampService();
        var handler = new ResetPasswordHandler(db, hasher, stamps, new FakeClock(FixedNow), new ResetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new ResetPasswordDto
        {
            Email = "reset-me@darwin.de",
            Token = "valid-token",
            NewPassword = "NewPass1A!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persisted = await db.Set<User>().AsNoTracking()
            .SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persisted.PasswordHash.Should().Be("hashed:NewPass1A!");
        persisted.SecurityStamp.Should().Be("new-stamp");

        var persistedToken = await db.Set<UserToken>().AsNoTracking()
            .SingleAsync(x => x.Id == token.Id, TestContext.Current.CancellationToken);
        persistedToken.UsedAtUtc.Should().Be(FixedNow);
    }

    [Fact]
    public async Task ResetPassword_Should_Fail_WhenUserNotFound()
    {
        await using var db = ResetPasswordTestDbContext.Create();
        var handler = new ResetPasswordHandler(db, new FakeHasher(), new FakeStampService(), new FakeClock(FixedNow), new ResetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new ResetPasswordDto
        {
            Email = "nobody@darwin.de",
            Token = "any-token",
            NewPassword = "NewPass1A!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidTokenOrEmail");
    }

    [Fact]
    public async Task ResetPassword_Should_Fail_WhenTokenNotFound()
    {
        await using var db = ResetPasswordTestDbContext.Create();
        var user = CreateUser("no-token@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ResetPasswordHandler(db, new FakeHasher(), new FakeStampService(), new FakeClock(FixedNow), new ResetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new ResetPasswordDto
        {
            Email = "no-token@darwin.de",
            Token = "missing-token",
            NewPassword = "NewPass1A!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidOrExpiredToken");
    }

    [Fact]
    public async Task ResetPassword_Should_Fail_WhenTokenAlreadyUsed()
    {
        await using var db = ResetPasswordTestDbContext.Create();
        var user = CreateUser("used-token@darwin.de");
        db.Set<User>().Add(user);

        var token = new UserToken(user.Id, "PasswordReset", "already-used-token", FixedNow.AddHours(1));
        token.MarkUsed(FixedNow.AddMinutes(-10));
        db.Set<UserToken>().Add(token);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ResetPasswordHandler(db, new FakeHasher(), new FakeStampService(), new FakeClock(FixedNow), new ResetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new ResetPasswordDto
        {
            Email = "used-token@darwin.de",
            Token = "already-used-token",
            NewPassword = "NewPass1A!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidOrExpiredToken");
    }

    [Fact]
    public async Task ResetPassword_Should_Fail_WhenTokenIsExpired()
    {
        await using var db = ResetPasswordTestDbContext.Create();
        var user = CreateUser("expired-token@darwin.de");
        db.Set<User>().Add(user);

        var token = new UserToken(user.Id, "PasswordReset", "expired-token", FixedNow.AddHours(-1));
        db.Set<UserToken>().Add(token);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ResetPasswordHandler(db, new FakeHasher(), new FakeStampService(), new FakeClock(FixedNow), new ResetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new ResetPasswordDto
        {
            Email = "expired-token@darwin.de",
            Token = "expired-token",
            NewPassword = "NewPass1A!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidOrExpiredToken");
    }

    [Fact]
    public async Task ResetPassword_Should_Fail_WhenUserIsDeleted()
    {
        await using var db = ResetPasswordTestDbContext.Create();
        var user = CreateUser("deleted@darwin.de");
        user.IsDeleted = true;
        db.Set<User>().Add(user);

        var token = new UserToken(user.Id, "PasswordReset", "some-token", FixedNow.AddHours(1));
        db.Set<UserToken>().Add(token);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ResetPasswordHandler(db, new FakeHasher(), new FakeStampService(), new FakeClock(FixedNow), new ResetPasswordValidator(new TestLocalizer()), new TestLocalizer());

        var result = await handler.HandleAsync(new ResetPasswordDto
        {
            Email = "deleted@darwin.de",
            Token = "some-token",
            NewPassword = "NewPass1A!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidTokenOrEmail");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(string email) =>
        new(email, "hashed:old-pass", "old-stamp")
        {
            FirstName = "Test",
            LastName = "User",
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

    private sealed class FakeHasher : IUserPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string hashedPassword, string password) => hashedPassword == $"hashed:{password}";
    }

    private sealed class FakeStampService : ISecurityStampService
    {
        public string NewStamp() => "new-stamp";
        public bool AreEqual(string? a, string? b) => string.Equals(a, b, StringComparison.Ordinal);
    }

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

    private sealed class ResetPasswordTestDbContext : DbContext, IAppDbContext
    {
        private ResetPasswordTestDbContext(DbContextOptions<ResetPasswordTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ResetPasswordTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ResetPasswordTestDbContext>()
                .UseInMemoryDatabase($"darwin_reset_password_tests_{Guid.NewGuid()}")
                .Options;
            return new ResetPasswordTestDbContext(options);
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

            modelBuilder.Entity<UserToken>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.UserId).IsRequired();
                builder.Property(x => x.Purpose).IsRequired();
                builder.Property(x => x.Value).IsRequired();
            });
        }
    }
}
