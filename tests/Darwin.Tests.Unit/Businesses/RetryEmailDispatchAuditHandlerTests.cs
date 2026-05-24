using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darwin.Tests.Unit.Businesses;

public sealed class RetryEmailDispatchAuditHandlerTests
{
    [Fact]
    public async Task RetryEmailDispatchAudit_Should_UseLatestInvitationRowVersion_WhenRetryingBusinessInvitation()
    {
        await using var db = RetryEmailDispatchAuditTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var oldInvitationId = Guid.NewGuid();
        var latestInvitationId = Guid.NewGuid();
        var auditId = Guid.NewGuid();
        var recipientEmail = "OWNER@DARWIN.DE";

        db.Set<Business>().Add(new Business
        {
            Id = businessId,
            Name = "Darwin HQ",
            DefaultCulture = "de-DE",
            DefaultCurrency = "EUR",
            IsActive = true
        });

        db.Set<BusinessInvitation>().AddRange(
            new BusinessInvitation
            {
                Id = oldInvitationId,
                BusinessId = businessId,
                InvitedByUserId = inviterId,
                Email = "owner@darwin.de",
                NormalizedEmail = "OWNER@DARWIN.DE",
                Role = BusinessMemberRole.Owner,
                Token = "old-token",
                ExpiresAtUtc = new DateTime(2030, 2, 3, 8, 0, 0, DateTimeKind.Utc),
                Status = BusinessInvitationStatus.Pending,
                CreatedAtUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                RowVersion = [1]
            },
            new BusinessInvitation
            {
                Id = latestInvitationId,
                BusinessId = businessId,
                InvitedByUserId = inviterId,
                Email = "owner@darwin.de",
                NormalizedEmail = "OWNER@DARWIN.DE",
                Role = BusinessMemberRole.Owner,
                Token = "current-token",
                ExpiresAtUtc = new DateTime(2030, 2, 4, 8, 0, 0, DateTimeKind.Utc),
                Status = BusinessInvitationStatus.Pending,
                CreatedAtUtc = new DateTime(2030, 1, 2, 8, 0, 0, DateTimeKind.Utc),
                RowVersion = [2]
            });

        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Id = auditId,
            FlowKey = "BusinessInvitation",
            BusinessId = businessId,
            Subject = "Business invite retry",
            RecipientEmail = $" {recipientEmail} ",
            Status = "Failed",
            AttemptedAtUtc = new DateTime(2030, 2, 5, 8, 0, 0, DateTimeKind.Utc),
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var clock = new FixedClock(new DateTime(2030, 2, 10, 8, 0, 0, DateTimeKind.Utc));
        var emailSender = new CapturingEmailSender();
        var resendHandler = new ResendBusinessInvitationHandler(
            db,
            emailSender,
            clock,
            new NullBusinessInvitationLinkBuilder(),
            new BusinessInvitationResendDtoValidator(),
            new TestStringLocalizer<ValidationResource>(),
            new TestStringLocalizer<CommunicationResource>());
        var requestEmailConfirmationHandler = new RequestEmailConfirmationHandler(
            db,
            emailSender,
            clock,
            new RequestEmailConfirmationValidator(),
            new TestStringLocalizer<CommunicationResource>(),
            NullLogger<RequestEmailConfirmationHandler>.Instance);
        var requestPasswordResetHandler = new RequestPasswordResetHandler(
            db,
            emailSender,
            clock,
            new RequestPasswordResetValidator(),
            new TestStringLocalizer<CommunicationResource>(),
            NullLogger<RequestPasswordResetHandler>.Instance);

        var handler = new RetryEmailDispatchAuditHandler(
            db,
            resendHandler,
            requestEmailConfirmationHandler,
            requestPasswordResetHandler,
            clock,
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(new RetryEmailDispatchAuditDto
        {
            AuditId = auditId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        emailSender.Messages.Should().HaveCount(1);
        emailSender.Messages[0].Context.Should().NotBeNull();
        emailSender.Messages[0].Context!.CorrelationKey.Should().Be(latestInvitationId.ToString("N"));

        var latestInvitationAfterRetry = await db.Set<BusinessInvitation>()
            .FirstAsync(x => x.Id == latestInvitationId, TestContext.Current.CancellationToken);
        var oldInvitationAfterRetry = await db.Set<BusinessInvitation>()
            .FirstAsync(x => x.Id == oldInvitationId, TestContext.Current.CancellationToken);

        latestInvitationAfterRetry.Token.Should().NotBe("current-token");
        oldInvitationAfterRetry.Token.Should().Be("old-token");
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class NullBusinessInvitationLinkBuilder : IBusinessInvitationLinkBuilder
    {
        public string? BuildAcceptanceLink(string token) => null;
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public System.Collections.Generic.List<(string Subject, string Body, EmailDispatchContext? Context)> Messages { get; } = new();

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default, EmailDispatchContext? context = null)
        {
            Messages.Add((subject, htmlBody, context));
            return Task.CompletedTask;
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

    private sealed class RetryEmailDispatchAuditTestDbContext : DbContext, IAppDbContext
    {
        private RetryEmailDispatchAuditTestDbContext(DbContextOptions<RetryEmailDispatchAuditTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static RetryEmailDispatchAuditTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RetryEmailDispatchAuditTestDbContext>()
                .UseInMemoryDatabase($"darwin_retry_email_dispatch_audit_tests_{Guid.NewGuid()}")
                .Options;

            return new RetryEmailDispatchAuditTestDbContext(options);
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
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<EmailDispatchAudit>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RecipientEmail).IsRequired();
                builder.Property(x => x.Subject).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Title).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.SupportedCulturesCsv).IsRequired();
                builder.Property(x => x.RowVersion).IsRowVersion();
            });
        }
    }
}
