using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers onboarding invitation emails that now include both a manual token and an optional magic link.
/// </summary>
public sealed class BusinessInvitationEmailHandlersTests
{
    [Fact]
    public async Task CreateBusinessInvitation_Should_SendMagicLink_WhenConfigured()
    {
        await using var db = BusinessInvitationEmailTestDbContext.Create();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Bistro Falken",
            DefaultCulture = "de-DE",
            DefaultCurrency = "EUR",
            IsActive = true
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var emailSender = new CapturingEmailSender();
        var handler = new CreateBusinessInvitationHandler(
            db,
            emailSender,
            new FixedClock(new DateTime(2030, 2, 1, 8, 0, 0, DateTimeKind.Utc)),
            new FixedCurrentUserService(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            new FixedBusinessInvitationLinkBuilder("darwin-business://InvitationAcceptance?token=MAGIC"),
            new BusinessInvitationCreateDtoValidator(),
            new TestValidationStringLocalizer(),
            new TestCommunicationStringLocalizer());

        await handler.HandleAsync(new BusinessInvitationCreateDto
        {
            BusinessId = businessId,
            Email = "owner@falken.de",
            Role = BusinessMemberRole.Owner,
            ExpiresInDays = 7
        }, TestContext.Current.CancellationToken);

        emailSender.LastBody.Should().Contain("Open your invitation");
        emailSender.LastBody.Should().Contain("darwin-business://InvitationAcceptance?token=MAGIC");
        emailSender.LastBody.Should().Contain("<code>");
        emailSender.LastContext.Should().NotBeNull();
        emailSender.LastContext!.FlowKey.Should().Be("BusinessInvitation");
        emailSender.LastContext.BusinessId.Should().Be(businessId);
    }

    [Fact]
    public async Task CreateBusinessInvitation_Should_UseConfiguredTemplates_WhenPresent()
    {
        await using var db = BusinessInvitationEmailTestDbContext.Create();
        var businessId = Guid.NewGuid();

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Studio Mitte",
            DefaultCulture = "de-DE",
            DefaultCurrency = "EUR",
            IsActive = true
        });
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            Title = "Darwin",
            DefaultCulture = "de-DE",
            SupportedCulturesCsv = "de-DE,en-US",
            ContactEmail = "ops@darwin.de",
            HomeSlug = "home",
            BusinessInvitationEmailSubjectTemplate = "Join {business_name} as {role}",
            BusinessInvitationEmailBodyTemplate = "<p>{invitation_action}</p><p>{invitation_intro_html}</p><p>{token}</p>",
            TransactionalEmailSubjectPrefix = "[Ops]"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var emailSender = new CapturingEmailSender();
        var handler = new CreateBusinessInvitationHandler(
            db,
            emailSender,
            new FixedClock(new DateTime(2030, 2, 1, 8, 0, 0, DateTimeKind.Utc)),
            new FixedCurrentUserService(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            new NullBusinessInvitationLinkBuilder(),
            new BusinessInvitationCreateDtoValidator(),
            new TestValidationStringLocalizer(),
            new TestCommunicationStringLocalizer());

        await handler.HandleAsync(new BusinessInvitationCreateDto
        {
            BusinessId = businessId,
            Email = "owner@mitte.de",
            Role = BusinessMemberRole.Owner,
            ExpiresInDays = 7
        }, TestContext.Current.CancellationToken);

        emailSender.LastSubject.Should().Be("[Ops] Join Studio Mitte as Owner");
        emailSender.LastBody.Should().Contain("invited");
        emailSender.LastBody.Should().Contain("Studio Mitte");
    }

    [Fact]
    public async Task ResendBusinessInvitation_Should_KeepManualToken_WhenMagicLinkMissing()
    {
        await using var db = BusinessInvitationEmailTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Cafe Elbe",
            DefaultCulture = "de-DE",
            DefaultCurrency = "EUR",
            IsActive = true
        });

        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = invitationId,
            BusinessId = businessId,
            InvitedByUserId = Guid.NewGuid(),
            Email = "manager@elbe.de",
            NormalizedEmail = "MANAGER@ELBE.DE",
            Role = BusinessMemberRole.Manager,
            Token = "old-token",
            ExpiresAtUtc = new DateTime(2030, 2, 3, 8, 0, 0, DateTimeKind.Utc),
            Status = BusinessInvitationStatus.Pending,
            RowVersion = [1]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var emailSender = new CapturingEmailSender();
        var handler = new ResendBusinessInvitationHandler(
            db,
            emailSender,
            new FixedClock(new DateTime(2030, 2, 1, 8, 0, 0, DateTimeKind.Utc)),
            new NullBusinessInvitationLinkBuilder(),
            new BusinessInvitationResendDtoValidator(),
            new TestValidationStringLocalizer(),
            new TestCommunicationStringLocalizer());

        await handler.HandleAsync(new BusinessInvitationResendDto
        {
            Id = invitationId,
            ExpiresInDays = 5,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        emailSender.LastBody.Should().Contain("<code>");
        emailSender.LastBody.Should().NotContain("Open your invitation");
        emailSender.LastContext.Should().NotBeNull();
        emailSender.LastContext!.FlowKey.Should().Be("BusinessInvitation");
        emailSender.LastContext.BusinessId.Should().Be(businessId);
    }

    [Fact]
    public async Task RevokeBusinessInvitation_Should_RejectAlreadyRevokedInvitation()
    {
        await using var db = BusinessInvitationEmailTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();

        db.Set<BusinessInvitation>().Add(new BusinessInvitation
        {
            Id = invitationId,
            BusinessId = businessId,
            InvitedByUserId = Guid.NewGuid(),
            Email = "manager@revoked.de",
            NormalizedEmail = "MANAGER@REVOKED.DE",
            Role = BusinessMemberRole.Manager,
            Token = "closed-token",
            ExpiresAtUtc = new DateTime(2030, 2, 3, 8, 0, 0, DateTimeKind.Utc),
            Status = BusinessInvitationStatus.Revoked,
            RowVersion = [1]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RevokeBusinessInvitationHandler(
            db,
            new FixedClock(new DateTime(2030, 2, 1, 8, 0, 0, DateTimeKind.Utc)),
            new BusinessInvitationRevokeDtoValidator(),
            new TestValidationStringLocalizer());

        var act = () => handler.HandleAsync(new BusinessInvitationRevokeDto
        {
            Id = invitationId,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("RevokedInvitationsCannotBeRevoked");
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public string LastSubject { get; private set; } = string.Empty;
        public string LastBody { get; private set; } = string.Empty;
        public EmailDispatchContext? LastContext { get; private set; }

        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default,
            EmailDispatchContext? context = null)
        {
            LastSubject = subject;
            LastBody = htmlBody;
            LastContext = context;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedCurrentUserService : ICurrentUserService
    {
        private readonly Guid _currentUserId;

        public FixedCurrentUserService(Guid currentUserId)
        {
            _currentUserId = currentUserId;
        }

        public Guid? TryGetCurrentUserId() => _currentUserId;

        public Guid GetCurrentUserId() => _currentUserId;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FixedBusinessInvitationLinkBuilder : IBusinessInvitationLinkBuilder
    {
        private readonly string _url;

        public FixedBusinessInvitationLinkBuilder(string url)
        {
            _url = url;
        }

        public string? BuildAcceptanceLink(string token) => _url;
    }

    private sealed class NullBusinessInvitationLinkBuilder : IBusinessInvitationLinkBuilder
    {
        public string? BuildAcceptanceLink(string token) => null;
    }

    private sealed class BusinessInvitationEmailTestDbContext : DbContext, IAppDbContext
    {
        private BusinessInvitationEmailTestDbContext(DbContextOptions<BusinessInvitationEmailTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessInvitationEmailTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessInvitationEmailTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_invitation_email_tests_{Guid.NewGuid()}")
                .Options;

            return new BusinessInvitationEmailTestDbContext(options);
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

            modelBuilder.Entity<BusinessMember>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Role).IsRequired();
            });

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRowVersion();
                builder.Property(x => x.Title).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.SupportedCulturesCsv).IsRequired();
            });
        }
    }

    private sealed class TestValidationStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class TestCommunicationStringLocalizer : IStringLocalizer<CommunicationResource>
    {
        public LocalizedString this[string name] => new(name, Resolve(name), resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(Resolve(name), arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;

        private static string Resolve(string name) => name switch
        {
            "BusinessInvitationSubjectTemplateDefault" => "You're invited to join {business_name}",
            "BusinessInvitationBodyTemplateDefault" =>
                "<p>Hello {recipient_name},</p><p>{invitation_intro_html}</p><p>{acceptance_link_html}</p><p>If you prefer, use this code instead:</p><p><code>{token}</code></p>",
            "BusinessInvitationAcceptanceLinkHtml" =>
                "<a href=\"{acceptance_link}\">Open your invitation</a>",
            "BusinessInvitationIntroInvitedHtml" =>
                "You have been invited to join <strong>{business_name}</strong> on Darwin.",
            "BusinessInvitationIntroReissuedHtml" =>
                "Your invitation to join <strong>{business_name}</strong> has been reissued.",
            "RecipientOverrideNoticeHtml" =>
                "<p><em>This message was routed to {override_recipient} because the configured recipient differs.</em></p>",
            _ => name
        };
    }
}
