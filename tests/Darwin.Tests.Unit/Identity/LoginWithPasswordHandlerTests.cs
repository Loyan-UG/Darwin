using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Security;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Covers password-login account-state enforcement for confirmation and lockout rules.
/// </summary>
public sealed class LoginWithPasswordHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_EmailIsNotConfirmed()
    {
        await using var db = LoginWithPasswordTestDbContext.Create();
        var user = CreateUser("unconfirmed-login@darwin.de");
        user.EmailConfirmed = false;

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtTokenService();
        var handler = new LoginWithPasswordHandler(db, jwt, new FakeLoginRateLimiter(), new FakeUserPasswordHasher());

        var result = await handler.HandleAsync(
            new PasswordLoginRequestDto
            {
                Email = user.Email,
                PasswordPlain = "Password123!"
            },
            "identity:unconfirmed-login",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Email address is not confirmed.");
        jwt.IssueTokensCalls.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_AccountIsLocked()
    {
        await using var db = LoginWithPasswordTestDbContext.Create();
        var user = CreateUser("locked-login@darwin.de");
        user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(5);

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtTokenService();
        var handler = new LoginWithPasswordHandler(db, jwt, new FakeLoginRateLimiter(), new FakeUserPasswordHasher());

        var result = await handler.HandleAsync(
            new PasswordLoginRequestDto
            {
                Email = user.Email,
                PasswordPlain = "Password123!"
            },
            "identity:locked-login",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Account is locked.");
        jwt.IssueTokensCalls.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_Should_IssueTokens_ForConfirmedUnlockedUser()
    {
        await using var db = LoginWithPasswordTestDbContext.Create();
        var user = CreateUser("confirmed-login@darwin.de");

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtTokenService();
        var handler = new LoginWithPasswordHandler(db, jwt, new FakeLoginRateLimiter(), new FakeUserPasswordHasher());

        var result = await handler.HandleAsync(
            new PasswordLoginRequestDto
            {
                Email = user.Email,
                PasswordPlain = "Password123!"
            },
            "identity:confirmed-login",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(user.Email);
        jwt.IssueTokensCalls.Should().Be(1);
    }

    private static User CreateUser(string email)
    {
        return new User(email, "hashed-password", "security-stamp")
        {
            FirstName = "Mila",
            LastName = "Wagner",
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

    private sealed class FakeUserPasswordHasher : IUserPasswordHasher
    {
        public string Hash(string password) => "hashed-password";

        public bool Verify(string hashedPassword, string password)
            => string.Equals(hashedPassword, "hashed-password", StringComparison.Ordinal) &&
               string.Equals(password, "Password123!", StringComparison.Ordinal);
    }

    private sealed class FakeLoginRateLimiter : ILoginRateLimiter
    {
        public Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task RecordAsync(string key, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public int IssueTokensCalls { get; private set; }

        public (string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc) IssueTokens(
            Guid userId,
            string email,
            string? deviceId,
            IEnumerable<string>? scopes = null,
            Guid? preferredBusinessId = null)
        {
            IssueTokensCalls++;
            return ("access-token", DateTime.UtcNow.AddMinutes(30), "refresh-token", DateTime.UtcNow.AddDays(7));
        }

        public Guid? ValidateRefreshToken(string refreshToken, string? deviceId) => null;

        public void RevokeRefreshToken(string refreshToken, string? deviceId)
        {
        }

        public int RevokeAllForUser(Guid userId) => 0;
    }

    private sealed class LoginWithPasswordTestDbContext : DbContext, IAppDbContext
    {
        private LoginWithPasswordTestDbContext(DbContextOptions<LoginWithPasswordTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static LoginWithPasswordTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LoginWithPasswordTestDbContext>()
                .UseInMemoryDatabase($"darwin_login_with_password_tests_{Guid.NewGuid()}")
                .Options;

            return new LoginWithPasswordTestDbContext(options);
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
