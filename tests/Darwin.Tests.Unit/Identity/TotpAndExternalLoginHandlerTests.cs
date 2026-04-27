using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Services;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Unit tests for TOTP and external login command handlers:
/// <see cref="TotpProvisioningHandler"/>, <see cref="EnableTotpHandler"/>,
/// <see cref="DisableTotpHandler"/>, <see cref="LinkExternalLoginHandler"/>,
/// and <see cref="UnlinkExternalLoginHandler"/>.
/// </summary>
public sealed class TotpAndExternalLoginHandlerTests
{
    // ─── TotpProvisioningHandler ──────────────────────────────────────────────

    [Fact]
    public async Task TotpProvisioning_Should_ReturnSecretAndUri_WhenUserExists()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TotpProvisioningHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpProvisionDto
        {
            UserId = userId,
            Issuer = "Darwin"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.SecretBase32.Should().NotBeNullOrEmpty();
        result.Value.OtpAuthUri.Should().StartWith("otpauth://totp/");
    }

    [Fact]
    public async Task TotpProvisioning_Should_Fail_WhenUserNotFound()
    {
        await using var db = TotpTestDbContext.Create();
        var handler = new TotpProvisioningHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpProvisionDto
        {
            UserId = Guid.NewGuid(),
            Issuer = "Darwin"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task TotpProvisioning_Should_ReplaceExistingInactiveSecret()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));

        // Pre-existing inactive secret
        db.Set<UserTwoFactorSecret>().Add(new UserTwoFactorSecret(userId, "OLDOLDSECRET", "test@example.com"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TotpProvisioningHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpProvisionDto
        {
            UserId = userId,
            Issuer = "Darwin"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var secrets = await db.Set<UserTwoFactorSecret>().ToListAsync(TestContext.Current.CancellationToken);
        secrets.Should().HaveCount(1, "old inactive secret should have been removed");
        secrets[0].SecretBase32.Should().Be(result.Value!.SecretBase32);
    }

    [Fact]
    public async Task TotpProvisioning_Should_UseEmailAsLabel_WhenNoOverrideProvided()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TotpProvisioningHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpProvisionDto
        {
            UserId = userId,
            Issuer = "Darwin"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.OtpAuthUri.Should().Contain("test%40example.com");
    }

    // ─── EnableTotpHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task EnableTotp_Should_ActivateSecret_WhenCodeIsValid()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        db.Set<User>().Add(user);

        var secret = TotpUtility.GenerateSecretBase32();
        db.Set<UserTwoFactorSecret>().Add(new UserTwoFactorSecret(userId, secret, "test@example.com"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new EnableTotpHandler(db, new TestLocalizer());
        var validCode = TotpUtility.ComputeTotpCode(secret, DateTime.UtcNow);

        var result = await handler.HandleAsync(new TotpEnableDto
        {
            UserId = userId,
            Code = validCode
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var updatedUser = await db.Set<User>().SingleAsync(TestContext.Current.CancellationToken);
        updatedUser.TwoFactorEnabled.Should().BeTrue();

        var secretEntity = await db.Set<UserTwoFactorSecret>().SingleAsync(TestContext.Current.CancellationToken);
        secretEntity.ActivatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task EnableTotp_Should_Fail_WhenUserNotFound()
    {
        await using var db = TotpTestDbContext.Create();
        var handler = new EnableTotpHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpEnableDto
        {
            UserId = Guid.NewGuid(),
            Code = 123456
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task EnableTotp_Should_Fail_WhenNoPendingSecret()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new EnableTotpHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpEnableDto
        {
            UserId = userId,
            Code = 123456
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("NoPendingTotpSecret");
    }

    [Fact]
    public async Task EnableTotp_Should_Fail_WhenCodeIsInvalid()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));

        var secret = TotpUtility.GenerateSecretBase32();
        db.Set<UserTwoFactorSecret>().Add(new UserTwoFactorSecret(userId, secret, "test@example.com"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new EnableTotpHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpEnableDto
        {
            UserId = userId,
            Code = 0 // very unlikely to be a valid code
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvalidTotpCode");
    }

    // ─── DisableTotpHandler ───────────────────────────────────────────────────

    [Fact]
    public async Task DisableTotp_Should_RemoveSecretsAndDisable2fa()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        user.TwoFactorEnabled = true;
        db.Set<User>().Add(user);

        db.Set<UserTwoFactorSecret>().Add(new UserTwoFactorSecret(userId, "SOMESECRET", "test@example.com"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DisableTotpHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpDisableDto { UserId = userId }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var updatedUser = await db.Set<User>().SingleAsync(TestContext.Current.CancellationToken);
        updatedUser.TwoFactorEnabled.Should().BeFalse();

        var secrets = await db.Set<UserTwoFactorSecret>().ToListAsync(TestContext.Current.CancellationToken);
        secrets.Should().BeEmpty("all TOTP secrets should be removed when 2FA is disabled");
    }

    [Fact]
    public async Task DisableTotp_Should_Fail_WhenUserNotFound()
    {
        await using var db = TotpTestDbContext.Create();
        var handler = new DisableTotpHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpDisableDto { UserId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task DisableTotp_Should_Succeed_WhenNoSecretsExist()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        user.TwoFactorEnabled = true;
        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DisableTotpHandler(db, new TestLocalizer());

        var result = await handler.HandleAsync(new TotpDisableDto { UserId = userId }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("disabling 2FA when no secrets exist should be idempotent");

        var updatedUser = await db.Set<User>().SingleAsync(TestContext.Current.CancellationToken);
        updatedUser.TwoFactorEnabled.Should().BeFalse();
    }

    // ─── LinkExternalLoginHandler ─────────────────────────────────────────────

    [Fact]
    public async Task LinkExternalLogin_Should_AddLink_WhenUserExistsAndLinkIsNew()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new LinkExternalLoginHandler(db, new LinkExternalLoginValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new LinkExternalLoginDto
        {
            UserId = userId,
            Provider = "Google",
            ProviderKey = "google-uid-12345",
            DisplayName = "Max Mustermann"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var link = await db.Set<UserLogin>().SingleAsync(TestContext.Current.CancellationToken);
        link.Provider.Should().Be("Google");
        link.ProviderKey.Should().Be("google-uid-12345");
        link.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task LinkExternalLogin_Should_Fail_WhenUserNotFound()
    {
        await using var db = TotpTestDbContext.Create();
        var handler = new LinkExternalLoginHandler(db, new LinkExternalLoginValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new LinkExternalLoginDto
        {
            UserId = Guid.NewGuid(),
            Provider = "Google",
            ProviderKey = "google-uid-99999"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task LinkExternalLogin_Should_BeIdempotent_WhenLinkAlreadyExists()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));
        db.Set<UserLogin>().Add(new UserLogin(userId, "Google", "google-uid-12345"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new LinkExternalLoginHandler(db, new LinkExternalLoginValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new LinkExternalLoginDto
        {
            UserId = userId,
            Provider = "Google",
            ProviderKey = "google-uid-12345"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("linking an already-existing provider/key should be idempotent");

        var count = await db.Set<UserLogin>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1, "no duplicate link should be created");
    }

    // ─── UnlinkExternalLoginHandler ───────────────────────────────────────────

    [Fact]
    public async Task UnlinkExternalLogin_Should_SoftDeleteLink_WhenLinkExists()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));
        db.Set<UserLogin>().Add(new UserLogin(userId, "Google", "google-uid-12345"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UnlinkExternalLoginHandler(db, new UnlinkExternalLoginValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UnlinkExternalLoginDto
        {
            UserId = userId,
            Provider = "Google",
            ProviderKey = "google-uid-12345"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var link = await db.Set<UserLogin>().SingleAsync(TestContext.Current.CancellationToken);
        link.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task UnlinkExternalLogin_Should_Fail_WhenLinkNotFound()
    {
        await using var db = TotpTestDbContext.Create();
        var handler = new UnlinkExternalLoginHandler(db, new UnlinkExternalLoginValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UnlinkExternalLoginDto
        {
            UserId = Guid.NewGuid(),
            Provider = "Google",
            ProviderKey = "no-such-key"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ExternalLinkNotFound");
    }

    [Fact]
    public async Task UnlinkExternalLogin_Should_Fail_WhenLinkIsAlreadySoftDeleted()
    {
        await using var db = TotpTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<User>().Add(CreateUser(userId));
        var login = new UserLogin(userId, "Google", "google-uid-12345") { IsDeleted = true };
        db.Set<UserLogin>().Add(login);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UnlinkExternalLoginHandler(db, new UnlinkExternalLoginValidator(), new TestLocalizer());

        var result = await handler.HandleAsync(new UnlinkExternalLoginDto
        {
            UserId = userId,
            Provider = "Google",
            ProviderKey = "google-uid-12345"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ExternalLinkNotFound");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User CreateUser(Guid id)
    {
        return new User("test@example.com", "hash", "stamp")
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            Locale = "de-DE",
            Currency = "EUR",
            Timezone = "Europe/Berlin",
            ChannelsOptInJson = "{}",
            FirstTouchUtmJson = "{}",
            LastTouchUtmJson = "{}",
            ExternalIdsJson = "{}"
        };
    }

    private sealed class TestLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, false);
        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class TotpTestDbContext : DbContext, IAppDbContext
    {
        private TotpTestDbContext(DbContextOptions<TotpTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static TotpTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<TotpTestDbContext>()
                .UseInMemoryDatabase($"darwin_totp_extlogin_tests_{Guid.NewGuid()}")
                .Options;
            return new TotpTestDbContext(options);
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

            modelBuilder.Entity<UserTwoFactorSecret>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.SecretBase32).IsRequired();
                builder.Property(x => x.Label).IsRequired();
                builder.Property(x => x.Issuer).IsRequired();
                builder.Ignore(x => x.User);
            });

            modelBuilder.Entity<UserLogin>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.ProviderKey).IsRequired();
                builder.Ignore(x => x.User);
            });
        }
    }
}
