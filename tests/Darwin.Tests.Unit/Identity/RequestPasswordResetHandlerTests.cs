using System;
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

public sealed class RequestPasswordResetHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_UseConfiguredTemplates_WhenPresent()
    {
        await using var db = PasswordResetTestDbContext.Create();
        var user = CreateUser("reset-template@darwin.de");

        db.Set<User>().Add(user);
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            Title = "Darwin",
            DefaultCulture = "de-DE",
            SupportedCulturesCsv = "de-DE,en-US",
            ContactEmail = "ops@darwin.de",
            HomeSlug = "home",
            TransactionalEmailSubjectPrefix = "[QA]",
            PasswordResetEmailSubjectTemplate = "Reset {email}",
            PasswordResetEmailBodyTemplate = "<p>{email}</p><p>{token}</p><p>{expires_at_utc}</p>"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var email = new FakeEmailSender();
        var handler = new RequestPasswordResetHandler(
            db,
            email,
            new FakeClock(new DateTime(2030, 3, 1, 10, 0, 0, DateTimeKind.Utc)),
            new RequestPasswordResetValidator(),
            new TestStringLocalizer<CommunicationResource>(),
            NullLogger<RequestPasswordResetHandler>.Instance);

        var result = await handler.HandleAsync(
            new RequestPasswordResetDto { Email = user.Email },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        email.Messages.Should().HaveCount(1);
        email.Messages[0].Subject.Should().Be("[QA] Reset reset-template@darwin.de");
        email.Messages[0].Body.Should().Contain("reset-template@darwin.de");
        email.Messages[0].Body.Should().Contain("2030-03-01 12:00:00Z");
        email.Messages[0].Context.Should().NotBeNull();
        email.Messages[0].Context!.FlowKey.Should().Be("PasswordReset");
    }

    private static User CreateUser(string email)
    {
        return new User(email, "hashed-password", "security-stamp")
        {
            FirstName = "Noah",
            LastName = "Weber",
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
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FakeEmailSender : IEmailSender
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

    private sealed class PasswordResetTestDbContext : DbContext, IAppDbContext
    {
        private PasswordResetTestDbContext(DbContextOptions<PasswordResetTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PasswordResetTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<PasswordResetTestDbContext>()
                .UseInMemoryDatabase($"darwin_password_reset_tests_{Guid.NewGuid()}")
                .Options;

            return new PasswordResetTestDbContext(options);
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
