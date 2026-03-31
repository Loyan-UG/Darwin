using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
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

namespace Darwin.Tests.Unit.Identity;

public sealed class PhoneVerificationHandlersTests
{
    [Fact]
    public async Task RequestPhoneVerification_Should_CreateSmsToken_AndSendSms()
    {
        await using var db = PhoneVerificationTestDbContext.Create();
        var user = CreateUser("phone-verify@darwin.de", "+4915112345678");
        db.Set<User>().Add(user);
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            Title = "Darwin",
            DefaultCulture = "de-DE",
            SupportedCulturesCsv = "de-DE,en-US",
            ContactEmail = "ops@darwin.de",
            HomeSlug = "home",
            SmsEnabled = true,
            SmsProvider = "Twilio",
            SmsFromPhoneE164 = "+4915000000000",
            PhoneVerificationSmsTemplate = "Code {token} for {phone_e164} until {expires_at_utc}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var smsSender = new FakeSmsSender();
        var handler = new RequestPhoneVerificationHandler(
            db,
            new StubCurrentUserService(user.Id),
            smsSender,
            new FakeWhatsAppSender(),
            new FakeClock(new DateTime(2030, 4, 1, 8, 0, 0, DateTimeKind.Utc)),
            new RequestPhoneVerificationValidator());

        var result = await handler.HandleAsync(
            new RequestPhoneVerificationDto { Channel = PhoneVerificationChannel.Sms },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        smsSender.Messages.Should().HaveCount(1);
        smsSender.Messages[0].ToPhone.Should().Be(user.PhoneE164);
        smsSender.Messages[0].Text.Should().Contain("+4915112345678");

        var token = await db.Set<UserToken>()
            .SingleAsync(x => x.UserId == user.Id && x.Purpose == "PhoneVerification", TestContext.Current.CancellationToken);

        token.UsedAtUtc.Should().BeNull();
        token.Value.Should().HaveLength(6);
        token.ExpiresAtUtc.Should().Be(new DateTime(2030, 4, 1, 8, 15, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ConfirmPhoneVerification_Should_MarkPhoneConfirmed_AndConsumeToken()
    {
        await using var db = PhoneVerificationTestDbContext.Create();
        var user = CreateUser("phone-confirm@darwin.de", "+4915112345678");
        user.PhoneNumberConfirmed = false;
        db.Set<User>().Add(user);
        db.Set<UserToken>().Add(new UserToken(user.Id, "PhoneVerification", "654321", new DateTime(2030, 4, 1, 8, 15, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ConfirmPhoneVerificationHandler(
            db,
            new StubCurrentUserService(user.Id),
            new FakeClock(new DateTime(2030, 4, 1, 8, 0, 0, DateTimeKind.Utc)),
            new ConfirmPhoneVerificationValidator());

        var result = await handler.HandleAsync(
            new ConfirmPhoneVerificationDto { Code = "654321" },
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();

        var persistedUser = await db.Set<User>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        persistedUser.PhoneNumberConfirmed.Should().BeTrue();

        var token = await db.Set<UserToken>()
            .AsNoTracking()
            .SingleAsync(x => x.UserId == user.Id && x.Purpose == "PhoneVerification", TestContext.Current.CancellationToken);
        token.UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RequestPhoneVerification_Should_UsePreferredWhatsApp_WhenChannelIsNotSpecified()
    {
        await using var db = PhoneVerificationTestDbContext.Create();
        var user = CreateUser("phone-policy@darwin.de", "+4915112345678");
        db.Set<User>().Add(user);
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            Title = "Darwin",
            DefaultCulture = "de-DE",
            SupportedCulturesCsv = "de-DE,en-US",
            ContactEmail = "ops@darwin.de",
            HomeSlug = "home",
            SmsEnabled = true,
            SmsProvider = "Twilio",
            SmsFromPhoneE164 = "+4915000000000",
            WhatsAppEnabled = true,
            WhatsAppBusinessPhoneId = "phone-id",
            WhatsAppAccessToken = "token",
            PhoneVerificationPreferredChannel = "WhatsApp",
            PhoneVerificationAllowFallback = true,
            PhoneVerificationSmsTemplate = "SMS {token}",
            PhoneVerificationWhatsAppTemplate = "WA {token}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var smsSender = new FakeSmsSender();
        var whatsAppSender = new FakeWhatsAppSender();
        var handler = new RequestPhoneVerificationHandler(
            db,
            new StubCurrentUserService(user.Id),
            smsSender,
            whatsAppSender,
            new FakeClock(new DateTime(2030, 4, 1, 8, 0, 0, DateTimeKind.Utc)),
            new RequestPhoneVerificationValidator());

        var result = await handler.HandleAsync(
            new RequestPhoneVerificationDto(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        smsSender.Messages.Should().BeEmpty();
        whatsAppSender.Messages.Should().HaveCount(1);
        whatsAppSender.Messages[0].ToPhone.Should().Be(user.PhoneE164);
        whatsAppSender.Messages[0].Text.Should().Contain("WA");
    }

    [Fact]
    public async Task RequestPhoneVerification_Should_FallbackToSms_WhenPreferredWhatsAppIsUnavailable()
    {
        await using var db = PhoneVerificationTestDbContext.Create();
        var user = CreateUser("phone-fallback@darwin.de", "+4915112345678");
        db.Set<User>().Add(user);
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Id = Guid.NewGuid(),
            Title = "Darwin",
            DefaultCulture = "de-DE",
            SupportedCulturesCsv = "de-DE,en-US",
            ContactEmail = "ops@darwin.de",
            HomeSlug = "home",
            SmsEnabled = true,
            SmsProvider = "Twilio",
            SmsFromPhoneE164 = "+4915000000000",
            WhatsAppEnabled = false,
            PhoneVerificationPreferredChannel = "WhatsApp",
            PhoneVerificationAllowFallback = true,
            PhoneVerificationSmsTemplate = "SMS {token}",
            PhoneVerificationWhatsAppTemplate = "WA {token}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var smsSender = new FakeSmsSender();
        var whatsAppSender = new FakeWhatsAppSender();
        var handler = new RequestPhoneVerificationHandler(
            db,
            new StubCurrentUserService(user.Id),
            smsSender,
            whatsAppSender,
            new FakeClock(new DateTime(2030, 4, 1, 8, 0, 0, DateTimeKind.Utc)),
            new RequestPhoneVerificationValidator());

        var result = await handler.HandleAsync(
            new RequestPhoneVerificationDto(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        smsSender.Messages.Should().HaveCount(1);
        whatsAppSender.Messages.Should().BeEmpty();
    }

    private static User CreateUser(string email, string phoneE164)
    {
        return new User(email, "hashed-password", "security-stamp")
        {
            FirstName = "Lina",
            LastName = "Schmidt",
            PhoneE164 = phoneE164,
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

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        private readonly Guid _userId;
        public StubCurrentUserService(Guid userId) => _userId = userId;
        public Guid GetCurrentUserId() => _userId;
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FakeSmsSender : ISmsSender
    {
        public System.Collections.Generic.List<(string ToPhone, string Text)> Messages { get; } = new();
        public Task SendAsync(string toPhoneE164, string text, CancellationToken ct = default, ChannelDispatchContext? context = null)
        {
            Messages.Add((toPhoneE164, text));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWhatsAppSender : IWhatsAppSender
    {
        public System.Collections.Generic.List<(string ToPhone, string Text)> Messages { get; } = new();
        public Task SendTextAsync(string toPhoneE164, string text, CancellationToken ct = default, ChannelDispatchContext? context = null)
        {
            Messages.Add((toPhoneE164, text));
            return Task.CompletedTask;
        }
    }

    private sealed class PhoneVerificationTestDbContext : DbContext, IAppDbContext
    {
        private PhoneVerificationTestDbContext(DbContextOptions<PhoneVerificationTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PhoneVerificationTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<PhoneVerificationTestDbContext>()
                .UseInMemoryDatabase($"darwin_phone_verification_tests_{Guid.NewGuid()}")
                .Options;

            return new PhoneVerificationTestDbContext(options);
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
