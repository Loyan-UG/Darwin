using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers business invitation preview and acceptance flows used by the Business mobile app.
/// </summary>
public sealed class BusinessInvitationOnboardingHandlersTests
{
    [Fact]
    public async Task GetBusinessInvitationPreview_Should_MapExistingUser_AndEffectiveStatus()
    {
        await using var db = BusinessInvitationTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var token = "preview-token-123456";
        var invitedEmail = "owner@bistro-aurora.de";

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Bistro Aurora",
            DefaultCulture = "de-DE",
            DefaultCurrency = "EUR",
            OperationalStatus = BusinessOperationalStatus.PendingApproval,
            IsActive = false
        });

        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = invitationId,
            BusinessId = businessId,
            InvitedByUserId = Guid.NewGuid(),
            Email = invitedEmail,
            NormalizedEmail = invitedEmail.ToUpperInvariant(),
            Role = BusinessMemberRole.Manager,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
            Status = BusinessInvitationStatus.Pending
        });

        db.Set<User>().Add(CreateUser(invitedEmail));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationPreviewHandler(db, new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(token, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.InvitationId.Should().Be(invitationId);
        result.Value.BusinessName.Should().Be("Bistro Aurora");
        result.Value.Role.Should().Be(nameof(BusinessMemberRole.Manager));
        result.Value.Status.Should().Be(nameof(BusinessInvitationStatus.Expired));
        result.Value.HasExistingUser.Should().BeTrue();
    }

    [Fact]
    public async Task GetBusinessInvitationPreview_Should_Fail_WhenBusinessIsSuspended()
    {
        await using var db = BusinessInvitationTestDbContext.Create();
        var token = "preview-token-suspended";

        db.Set<Business>().Add(new Business
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Suspended Business",
            DefaultCulture = "de-DE",
            DefaultCurrency = "EUR",
            OperationalStatus = BusinessOperationalStatus.Suspended,
            IsActive = false
        });

        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            InvitedByUserId = Guid.NewGuid(),
            Email = "owner@suspended.test",
            NormalizedEmail = "OWNER@SUSPENDED.TEST",
            Role = BusinessMemberRole.Owner,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            Status = BusinessInvitationStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessInvitationPreviewHandler(db, new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(token, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("InvitedBusinessUnavailable");
    }

    [Fact]
    public async Task AcceptBusinessInvitation_Should_CreateUserMembership_AndIssuePreferredBusinessToken()
    {
        await using var db = BusinessInvitationTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var invitationToken = "accept-token-123456";
        var businessRoleId = Guid.NewGuid();

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Cafe Morgenrot",
            DefaultCulture = "de-DE",
            DefaultCurrency = "EUR",
            OperationalStatus = BusinessOperationalStatus.PendingApproval,
            IsActive = false
        });

        db.Set<Role>().Add(new Role("business", "Business", isSystem: true, description: null)
        {
            Id = businessRoleId
        });

        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            InvitedByUserId = Guid.NewGuid(),
            Email = "operator@morgenrot.de",
            NormalizedEmail = "OPERATOR@MORGENROT.DE",
            Role = BusinessMemberRole.Owner,
            Token = invitationToken,
            ExpiresAtUtc = new DateTime(2030, 1, 2, 10, 0, 0, DateTimeKind.Utc),
            Status = BusinessInvitationStatus.Pending
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var jwt = new FakeJwtTokenService();
        var handler = new AcceptBusinessInvitationHandler(
            db,
            new FakePasswordHasher(),
            new FakeSecurityStampService(),
            jwt,
            new FakeClock(new DateTime(2030, 1, 1, 9, 0, 0, DateTimeKind.Utc)),
            new BusinessInvitationAcceptDtoValidator(),
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(new BusinessInvitationAcceptDto
        {
            Token = invitationToken,
            DeviceId = "device-1",
            FirstName = "Greta",
            LastName = "Sommer",
            Password = "Business123!"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.BusinessId.Should().Be(businessId);
        result.Value.BusinessName.Should().Be("Cafe Morgenrot");
        result.Value.IsNewUser.Should().BeTrue();

        var persistedUser = await db.Set<User>()
            .SingleAsync(x => x.Email == "operator@morgenrot.de", TestContext.Current.CancellationToken);
        persistedUser.FirstName.Should().Be("Greta");
        persistedUser.LastName.Should().Be("Sommer");
        persistedUser.EmailConfirmed.Should().BeTrue();
        persistedUser.Locale.Should().Be("de-DE");
        persistedUser.Timezone.Should().Be("Europe/Berlin");
        persistedUser.Currency.Should().Be("EUR");

        var membership = await db.Set<BusinessMember>()
            .SingleAsync(x => x.BusinessId == businessId && x.UserId == persistedUser.Id, TestContext.Current.CancellationToken);
        membership.Role.Should().Be(BusinessMemberRole.Owner);
        membership.IsActive.Should().BeTrue();

        var userRole = await db.Set<UserRole>()
            .SingleAsync(x => x.UserId == persistedUser.Id && x.RoleId == businessRoleId, TestContext.Current.CancellationToken);
        userRole.IsDeleted.Should().BeFalse();

        var invitation = await db.Set<BusinessInvitation>()
            .SingleAsync(x => x.Token == invitationToken, TestContext.Current.CancellationToken);
        invitation.Status.Should().Be(BusinessInvitationStatus.Accepted);
        invitation.AcceptedByUserId.Should().Be(persistedUser.Id);

        jwt.LastPreferredBusinessId.Should().Be(businessId);
        jwt.LastDeviceId.Should().Be("device-1");
        jwt.LastEmail.Should().Be("operator@morgenrot.de");
    }

    private static User CreateUser(string email)
    {
        return new User(email, "hashed-password", "security-stamp")
        {
            FirstName = "Helena",
            LastName = "Fischer",
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

    private sealed class FakePasswordHasher : IUserPasswordHasher
    {
        public string Hash(string password) => $"hashed::{password}";

        public bool Verify(string hashedPassword, string password)
            => string.Equals(hashedPassword, Hash(password), StringComparison.Ordinal);
    }

    private sealed class FakeSecurityStampService : ISecurityStampService
    {
        public string NewStamp() => "generated-stamp";

        public bool AreEqual(string? a, string? b)
            => string.Equals(a, b, StringComparison.Ordinal);
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public Guid? LastPreferredBusinessId { get; private set; }

        public string? LastDeviceId { get; private set; }

        public string? LastEmail { get; private set; }

        public Task<(string accessToken, DateTime expiresAtUtc, string refreshToken, DateTime refreshExpiresAtUtc)> IssueTokensAsync(
            Guid userId,
            string email,
            string? deviceId,
            IEnumerable<string>? scopes = null,
            Guid? preferredBusinessId = null,
            CancellationToken ct = default)
        {
            LastPreferredBusinessId = preferredBusinessId;
            LastDeviceId = deviceId;
            LastEmail = email;

            return Task.FromResult(("access-token", DateTime.UtcNow.AddMinutes(30), "refresh-token", DateTime.UtcNow.AddDays(7)));
        }

        public Task<Guid?> ValidateRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);

        public Task RevokeRefreshTokenAsync(string refreshToken, string? deviceId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(0);
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

    private sealed class BusinessInvitationTestDbContext : DbContext, IAppDbContext
    {
        private BusinessInvitationTestDbContext(DbContextOptions<BusinessInvitationTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessInvitationTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessInvitationTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_invitation_tests_{Guid.NewGuid()}")
                .Options;

            return new BusinessInvitationTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
            });

            modelBuilder.Entity<BusinessInvitation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Email).IsRequired();
                builder.Property(x => x.NormalizedEmail).IsRequired();
                builder.Property(x => x.Token).IsRequired();
                builder.Property(x => x.Role).IsRequired();
                builder.Property(x => x.Status).IsRequired();
            });

            modelBuilder.Entity<BusinessMember>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Role).IsRequired();
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
            });

            modelBuilder.Entity<Role>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Key).IsRequired();
                builder.Property(x => x.NormalizedName).IsRequired();
                builder.Property(x => x.DisplayName).IsRequired();
            });

            modelBuilder.Entity<UserRole>(builder =>
            {
                builder.HasKey(x => x.Id);
            });
        }
    }
}
