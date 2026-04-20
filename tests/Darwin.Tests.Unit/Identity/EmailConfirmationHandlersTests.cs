using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darwin.Tests.Unit.Identity;

/// <summary>
/// Covers request and confirmation flows for email-confirmation tokens.
/// </summary>
public sealed class EmailConfirmationHandlersTests
{
    [Fact]
    public async Task RequestEmailConfirmation_Should_CreateToken_AndSendEmail_ForUnconfirmedUser()
    {
        await using var db = EmailConfirmationTestDbContext.Create();
        var user = CreateUser("confirm-request@darwin.de");
        user.EmailConfirmed = false;

        db.Set<User>().Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var email = new FakeEmailSender();
        var utcNow = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var handler = new RequestEmailConfirmationHandler(
            db,
            email,
            new FakeClock(utcNow),
            new RequestEmailConfirmationValidator(),
            new TestStringLocalizer<CommunicationResource>(),
            NullLogger<RequestEmailConfirmationHandler>.Instance);

        var result = await handler.HandleAsync(
            new RequestEmailConfirmationDto { Email = user.Email },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        email.SentMessages.Should().HaveCount(1);
        email.SentMessages[0].Context.Should().NotBeNull();
        email.SentMessages[0].Context!.FlowKey.Should().Be("AccountActivation");

        var token = await db.Set<UserToken>()
            .SingleAsync(x => x.UserId == user.Id && x.Purpose == "EmailConfirmation", TestContext.Current.CancellationToken);

        token.UsedAtUtc.Should().BeNull();
        token.ExpiresAtUtc.Should().Be(utcNow.AddHours(24));
        token.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConfirmEmail_Should_MarkUserConfirmed_AndConsumeToken()
    {
        await using var db = EmailConfirmationTestDbContext.Create();
        var user = CreateUser("confirm-complete@darwin.de");
        user.EmailConfirmed = false;

        db.Set<User>().Add(user);
        db.Set<UserToken>().Add(new UserToken(user.Id, "EmailConfirmation", "token-123", new DateTime(2030, 1, 2, 8, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConfirmEmailHandler(
            db,
            new FakeClock(new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc)),
            new ConfirmEmailValidator(),
            new TestStringLocalizer<ValidationResource>());

        var result = await handler.HandleAsync(
            new ConfirmEmailDto
            {
                Email = user.Email,
                Token = "token-123"
            },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persistedUser = await db.Set<User>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persistedUser.EmailConfirmed.Should().BeTrue();

        var persistedToken = await db.Set<UserToken>()
            .AsNoTracking()
            .SingleAsync(x => x.UserId == user.Id && x.Purpose == "EmailConfirmation", TestContext.Current.CancellationToken);
        persistedToken.UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RequestEmailConfirmation_Should_UseConfiguredTemplates_WhenPresent()
    {
        await using var db = EmailConfirmationTestDbContext.Create();
        var user = CreateUser("confirm-template@darwin.de");
        user.EmailConfirmed = false;

        db.Set<User>().Add(user);
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            Title = "Darwin",
            DefaultCulture = "de-DE",
            SupportedCulturesCsv = "de-DE,en-US",
            ContactEmail = "ops@darwin.de",
            HomeSlug = "home",
            TransactionalEmailSubjectPrefix = "[Stage]",
            AccountActivationEmailSubjectTemplate = "Activate {email}",
            AccountActivationEmailBodyTemplate = "<p>{email}</p><p>{token}</p><p>{expires_at_utc}</p>"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var email = new FakeEmailSender();
        var utcNow = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var handler = new RequestEmailConfirmationHandler(
            db,
            email,
            new FakeClock(utcNow),
            new RequestEmailConfirmationValidator(),
            new TestStringLocalizer<CommunicationResource>(),
            NullLogger<RequestEmailConfirmationHandler>.Instance);

        var result = await handler.HandleAsync(
            new RequestEmailConfirmationDto { Email = user.Email },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        email.SentMessages.Should().HaveCount(1);
        email.SentMessages[0].Subject.Should().Be("[Stage] Activate confirm-template@darwin.de");
        email.SentMessages[0].Body.Should().Contain("confirm-template@darwin.de");
        email.SentMessages[0].Body.Should().Contain("2030-01-02 08:00:00Z");
    }

    private static User CreateUser(string email)
    {
        return new User(email, "hashed-password", "security-stamp")
        {
            FirstName = "Mia",
            LastName = "Schneider",
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

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public System.Collections.Generic.List<(string To, string Subject, string Body, EmailDispatchContext? Context)> SentMessages { get; } = new();

        public Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default,
            EmailDispatchContext? context = null)
        {
            SentMessages.Add((toEmail, subject, htmlBody, context));
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

    private sealed class EmailConfirmationTestDbContext : DbContext, IAppDbContext
    {
        private EmailConfirmationTestDbContext(DbContextOptions<EmailConfirmationTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static EmailConfirmationTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<EmailConfirmationTestDbContext>()
                .UseInMemoryDatabase($"darwin_email_confirmation_tests_{Guid.NewGuid()}")
                .Options;

            return new EmailConfirmationTestDbContext(options);
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

            modelBuilder.Entity<UserToken>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Purpose).IsRequired();
                builder.Property(x => x.Value).IsRequired();
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
}
