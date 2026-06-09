using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Covers secure external-login account creation and linking policy.
/// </summary>
public sealed class LoginWithExternalProviderHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_CreateConfirmedMemberAndExternalLink_WhenVerifiedGoogleUserIsNew()
    {
        await using var db = ExternalLoginTestDbContext.Create();
        db.Set<Role>().Add(new Role("Members", "Members", isSystem: true, description: null) { Id = Guid.NewGuid() });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtTokenService();
        var handler = CreateHandler(db, jwt, new ExternalIdentityDto
        {
            Provider = "Google",
            ProviderKey = "google-sub-1",
            Email = "new-google@darwin.test",
            EmailVerified = true,
            FirstName = "Nora",
            LastName = "Keller",
            DisplayName = "Nora Keller"
        });

        var result = await handler.HandleAsync(new ExternalLoginRequestDto
        {
            Provider = "Google",
            IdToken = "valid-id-token",
            DeviceId = "device-1"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        jwt.IssueTokensCalls.Should().Be(1);

        var user = await db.Set<User>().SingleAsync(TestContext.Current.CancellationToken);
        user.EmailConfirmed.Should().BeTrue();
        user.Email.Should().Be("new-google@darwin.test");
        user.FirstName.Should().Be("Nora");

        var login = await db.Set<UserLogin>().SingleAsync(TestContext.Current.CancellationToken);
        login.UserId.Should().Be(user.Id);
        login.Provider.Should().Be("Google");
        login.ProviderKey.Should().Be("google-sub-1");

        (await db.Set<UserRole>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_Should_LinkExistingConfirmedUser_WhenVerifiedEmailMatches()
    {
        await using var db = ExternalLoginTestDbContext.Create();
        var user = CreateUser("confirmed-google@darwin.test", emailConfirmed: true);
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtTokenService();
        var handler = CreateHandler(db, jwt, new ExternalIdentityDto
        {
            Provider = "Google",
            ProviderKey = "google-sub-confirmed",
            Email = user.Email,
            EmailVerified = true,
            DisplayName = "Confirmed User"
        });

        var result = await handler.HandleAsync(new ExternalLoginRequestDto
        {
            Provider = "Google",
            IdToken = "valid-id-token"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        jwt.IssueTokensCalls.Should().Be(1);
        (await db.Set<User>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await db.Set<UserLogin>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_Should_RejectExistingUnconfirmedUser_WhenVerifiedEmailMatches()
    {
        await using var db = ExternalLoginTestDbContext.Create();
        var user = CreateUser("unconfirmed-google@darwin.test", emailConfirmed: false);
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtTokenService();
        var handler = CreateHandler(db, jwt, new ExternalIdentityDto
        {
            Provider = "Google",
            ProviderKey = "google-sub-unconfirmed",
            Email = user.Email,
            EmailVerified = true,
            DisplayName = "Unconfirmed User"
        });

        var result = await handler.HandleAsync(new ExternalLoginRequestDto
        {
            Provider = "Google",
            IdToken = "valid-id-token"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("EmailAddressNotConfirmed");
        jwt.IssueTokensCalls.Should().Be(0);
        (await db.Set<UserLogin>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    private static LoginWithExternalProviderHandler CreateHandler(
        ExternalLoginTestDbContext db,
        FakeJwtTokenService jwt,
        ExternalIdentityDto identity)
    {
        return new LoginWithExternalProviderHandler(
            db,
            new FakeExternalIdentityVerifier(identity),
            jwt,
            new FakeUserPasswordHasher(),
            new FakeSecurityStampService(),
            new ExternalLoginRequestValidator(),
            new TestStringLocalizer<ValidationResource>());
    }

    private static User CreateUser(string email, bool emailConfirmed)
    {
        return new User(email, "hashed-password", "security-stamp")
        {
            Id = Guid.NewGuid(),
            FirstName = "Mila",
            LastName = "Wagner",
            IsActive = true,
            EmailConfirmed = emailConfirmed,
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

    private sealed class FakeExternalIdentityVerifier : IExternalIdentityVerifier
    {
        private readonly ExternalIdentityDto _identity;

        public FakeExternalIdentityVerifier(ExternalIdentityDto identity) => _identity = identity;

        public Task<Result<ExternalIdentityDto>> VerifyAsync(ExternalLoginRequestDto request, CancellationToken ct = default)
            => Task.FromResult(Result<ExternalIdentityDto>.Ok(_identity));
    }

    private sealed class FakeUserPasswordHasher : IUserPasswordHasher
    {
        public string Hash(string password) => "hashed-external-password";
        public bool Verify(string hashedPassword, string password) => false;
    }

    private sealed class FakeSecurityStampService : ISecurityStampService
    {
        public string NewStamp() => "external-security-stamp";
        public bool AreEqual(string? a, string? b) => string.Equals(a, b, StringComparison.Ordinal);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public int IssueTokensCalls { get; private set; }

        public Task<(string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)> IssueTokensAsync(
            Guid userId,
            string email,
            string? deviceId,
            IEnumerable<string>? scopes = null,
            Guid? preferredBusinessId = null,
            CancellationToken ct = default)
        {
            IssueTokensCalls++;
            return Task.FromResult(("access-token", DateTime.UtcNow.AddMinutes(30), "refresh-token", DateTime.UtcNow.AddDays(7)));
        }

        public Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public Task RevokeRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class TestStringLocalizer<TResource> : IStringLocalizer<TResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class ExternalLoginTestDbContext : DbContext, IAppDbContext
    {
        private ExternalLoginTestDbContext(DbContextOptions<ExternalLoginTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ExternalLoginTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ExternalLoginTestDbContext>()
                .UseInMemoryDatabase($"darwin_external_login_tests_{Guid.NewGuid()}")
                .Options;

            return new ExternalLoginTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.HasMany(x => x.Logins).WithOne(x => x.User).HasForeignKey(x => x.UserId);
            });

            modelBuilder.Entity<UserLogin>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.HasOne(x => x.User).WithMany(x => x.Logins).HasForeignKey(x => x.UserId);
            });

            modelBuilder.Entity<Role>().HasKey(x => x.Id);
            modelBuilder.Entity<UserRole>().HasKey(x => x.Id);
        }
    }
}
