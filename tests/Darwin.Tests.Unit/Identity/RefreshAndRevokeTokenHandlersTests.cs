using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for <see cref="RefreshTokenHandler"/> and <see cref="RevokeRefreshTokensHandler"/>.
/// </summary>
public sealed class RefreshAndRevokeTokenHandlersTests
{
    // ─── RefreshTokenHandler ──────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_Should_ReturnNewTokens_WhenRefreshTokenIsValid()
    {
        await using var db = RefreshRevokeTestDbContext.Create();
        var user = CreateUser("user@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtService(user.Id);
        var handler = new RefreshTokenHandler(db, jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RefreshRequestDto
        {
            RefreshToken = "valid-refresh-token",
            DeviceId = "device-1"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be("user@darwin.de");
    }

    [Fact]
    public async Task RefreshToken_Should_Fail_WhenRefreshTokenIsInvalid()
    {
        await using var db = RefreshRevokeTestDbContext.Create();
        var jwt = new FakeJwtService(null); // returns null from ValidateRefreshToken
        var handler = new RefreshTokenHandler(db, jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RefreshRequestDto
        {
            RefreshToken = "invalid-token"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidOrExpiredRefreshToken");
    }

    [Fact]
    public async Task RefreshToken_Should_Fail_WhenUserNotFoundOrInactive()
    {
        await using var db = RefreshRevokeTestDbContext.Create();
        // JWT returns a userId but no matching user in DB
        var jwt = new FakeJwtService(Guid.NewGuid());
        var handler = new RefreshTokenHandler(db, jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RefreshRequestDto
        {
            RefreshToken = "valid-refresh-token"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task RefreshToken_Should_Fail_WhenUserIsInactive()
    {
        await using var db = RefreshRevokeTestDbContext.Create();
        var user = CreateUser("inactive@darwin.de");
        user.IsActive = false;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtService(user.Id);
        var handler = new RefreshTokenHandler(db, jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RefreshRequestDto
        {
            RefreshToken = "valid-refresh-token"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task RefreshToken_Should_Fail_WhenUserIsSoftDeleted()
    {
        await using var db = RefreshRevokeTestDbContext.Create();
        var user = CreateUser("deleted@darwin.de");
        user.IsDeleted = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtService(user.Id);
        var handler = new RefreshTokenHandler(db, jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RefreshRequestDto
        {
            RefreshToken = "valid-refresh-token"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFoundOrInactive");
    }

    [Fact]
    public async Task RefreshToken_Should_RevokeOldToken_BeforeIssuingNew()
    {
        await using var db = RefreshRevokeTestDbContext.Create();
        var user = CreateUser("rotation@darwin.de");
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtService(user.Id);
        var handler = new RefreshTokenHandler(db, jwt, new TestLocalizer());

        await handler.HandleAsync(new RefreshRequestDto
        {
            RefreshToken = "old-token",
            DeviceId = "my-device"
        }, TestContext.Current.CancellationToken);

        jwt.RevokedTokens.Should().ContainSingle(t => t.Token == "old-token" && t.DeviceId == "my-device");
    }

    // ─── RevokeRefreshTokensHandler ───────────────────────────────────────────

    [Fact]
    public async Task RevokeRefreshTokens_Should_RevokeSingleToken_WhenRefreshTokenProvided()
    {
        var jwt = new FakeJwtService(null);
        var handler = new RevokeRefreshTokensHandler(jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RevokeRefreshRequestDto
        {
            RefreshToken = "token-to-revoke",
            DeviceId = "device-1"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(1);
        jwt.RevokedTokens.Should().ContainSingle(t => t.Token == "token-to-revoke" && t.DeviceId == "device-1");
    }

    [Fact]
    public async Task RevokeRefreshTokens_Should_RevokeAllForUser_WhenUserIdProvided()
    {
        var userId = Guid.NewGuid();
        var jwt = new FakeJwtService(null, revokeAllCount: 5);
        var handler = new RevokeRefreshTokensHandler(jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RevokeRefreshRequestDto
        {
            UserId = userId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(5);
        jwt.RevokedUserIds.Should().ContainSingle(id => id == userId);
    }

    [Fact]
    public async Task RevokeRefreshTokens_Should_Fail_WhenNeitherTokenNorUserIdProvided()
    {
        var jwt = new FakeJwtService(null);
        var handler = new RevokeRefreshTokensHandler(jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RevokeRefreshRequestDto(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("NothingToRevoke");
    }

    [Fact]
    public async Task RevokeRefreshTokens_Should_PreferTokenRevocation_WhenBothProvided()
    {
        var jwt = new FakeJwtService(null);
        var handler = new RevokeRefreshTokensHandler(jwt, new TestLocalizer());

        var result = await handler.HandleAsync(new RevokeRefreshRequestDto
        {
            RefreshToken = "specific-token",
            UserId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(1);
        jwt.RevokedTokens.Should().ContainSingle(t => t.Token == "specific-token");
        jwt.RevokedUserIds.Should().BeEmpty();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(string email)
    {
        return new User(email, "hashed:initial", "initial-stamp")
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
    }

    private sealed class FakeJwtService : IJwtTokenService
    {
        private readonly Guid? _validatedUserId;
        private readonly int _revokeAllCount;

        public List<(string Token, string? DeviceId)> RevokedTokens { get; } = new();
        public List<Guid> RevokedUserIds { get; } = new();

        public FakeJwtService(Guid? validatedUserId, int revokeAllCount = 0)
        {
            _validatedUserId = validatedUserId;
            _revokeAllCount = revokeAllCount;
        }

        public (string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc) IssueTokens(
            Guid userId, string email, string? deviceId,
            IEnumerable<string>? scopes = null,
            Guid? preferredBusinessId = null)
            => ("access-token", DateTime.UtcNow.AddMinutes(30), "refresh-token", DateTime.UtcNow.AddDays(7));

        public Guid? ValidateRefreshToken(string refreshToken, string? deviceId) => _validatedUserId;

        public void RevokeRefreshToken(string refreshToken, string? deviceId)
            => RevokedTokens.Add((refreshToken, deviceId));

        public int RevokeAllForUser(Guid userId)
        {
            RevokedUserIds.Add(userId);
            return _revokeAllCount;
        }
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class RefreshRevokeTestDbContext : DbContext, IAppDbContext
    {
        private RefreshRevokeTestDbContext(DbContextOptions<RefreshRevokeTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static RefreshRevokeTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RefreshRevokeTestDbContext>()
                .UseInMemoryDatabase($"darwin_refresh_revoke_tests_{Guid.NewGuid()}")
                .Options;
            return new RefreshRevokeTestDbContext(options);
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
        }
    }
}
