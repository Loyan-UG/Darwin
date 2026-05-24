using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Notifications.Sms;
using Darwin.Infrastructure.Notifications.WhatsApp;
using Darwin.Infrastructure.Notifications.Smtp;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Tests.Notifications;

public sealed class SiteSettingTransportSoftDeleteTests
{
    [Fact]
    public async Task ProviderBackedSmsSender_Should_Ignore_SoftDeleted_SiteSetting_WhenActive_Row_Exists()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<SiteSetting>().AddRange(
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Legacy",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "legacy@darwin.de",
                HomeSlug = "home",
                IsDeleted = true,
                SmsEnabled = true,
                SmsProvider = "Twilio"
            },
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Darwin",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "ops@darwin.de",
                HomeSlug = "home",
                SmsEnabled = true,
                SmsProvider = "Twilio",
                SmsApiKey = "active-api-key",
                SmsApiSecret = "active-api-secret",
                SmsFromPhoneE164 = "+4915000000000"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var capturedRequest = new CapturingHttpMessageHandler();
        capturedRequest.Configure(
            (request, ct) =>
            {
                request!.Headers.TryGetValues("Authorization", out _).Should().BeTrue();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"sid":"SM_TEST"}""", Encoding.UTF8, "application/json")
                };
            });

        var sender = new ProviderBackedSmsSender(
            db,
            new FakeHttpClientFactory(capturedRequest),
            NullLogger<ProviderBackedSmsSender>.Instance,
            new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        var act = () => sender.SendAsync("+491511111111", "SMS-OK", TestContext.Current.CancellationToken);
        await act.Should().NotThrowAsync();

        var audit = await db.Set<ChannelDispatchAudit>().SingleAsync(TestContext.Current.CancellationToken);
        audit.Provider.Should().Be("Twilio");
        audit.Status.Should().Be("Sent");
        capturedRequest.Requests.Should().HaveCount(1);
        capturedRequest.Requests[0].RequestUri!.AbsoluteUri.Should().Contain("active-api-key");
    }

    [Fact]
    public async Task MetaWhatsAppSender_Should_Ignore_SoftDeleted_SiteSetting_WhenActive_Row_Exists()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<SiteSetting>().AddRange(
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Legacy",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "legacy@darwin.de",
                HomeSlug = "home",
                IsDeleted = true,
                WhatsAppEnabled = true,
                WhatsAppBusinessPhoneId = "legacy-phone-id",
                WhatsAppAccessToken = "legacy-token"
            },
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Darwin",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "ops@darwin.de",
                HomeSlug = "home",
                WhatsAppEnabled = true,
                WhatsAppBusinessPhoneId = "active-phone-id",
                WhatsAppAccessToken = "active-token"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var capturedRequest = new CapturingHttpMessageHandler();
        capturedRequest.Configure(
            (request, _) =>
            {
                request!.Headers.Authorization?.Scheme.Should().Be("Bearer");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"messages":[{"id":"wamid.test"}]}""", Encoding.UTF8, "application/json")
                };
            });

        var sender = new MetaWhatsAppSender(
            db,
            new FakeHttpClientFactory(capturedRequest),
            NullLogger<MetaWhatsAppSender>.Instance,
            new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        var act = () => sender.SendTextAsync("+491511111111", "WA-OK", TestContext.Current.CancellationToken);
        await act.Should().NotThrowAsync();

        var audit = await db.Set<ChannelDispatchAudit>().SingleAsync(TestContext.Current.CancellationToken);
        audit.Provider.Should().Be("Meta");
        audit.Status.Should().Be("Sent");
        capturedRequest.Requests.Should().HaveCount(1);
        capturedRequest.Requests[0].RequestUri!.AbsolutePath.Should().Contain("active-phone-id");
    }

    [Fact]
    public async Task ProviderBackedSmsSender_Should_CreateAuditClaim_BeforeTransportFailure()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<SiteSetting>().Add(
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Darwin",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "ops@darwin.de",
                HomeSlug = "home",
                SmsEnabled = true,
                SmsProvider = "Twilio",
                SmsApiKey = "active-api-key",
                SmsApiSecret = "active-api-secret",
                SmsFromPhoneE164 = "+4915000000000"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var capturedRequest = new CapturingHttpMessageHandler();
        capturedRequest.Configure(
            (_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("""{"error":"temporary"}""", Encoding.UTF8, "application/json")
            });

        var clock = new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        var sender = new ProviderBackedSmsSender(
            db,
            new FakeHttpClientFactory(capturedRequest),
            NullLogger<ProviderBackedSmsSender>.Instance,
            clock);

        var act = () => sender.SendAsync(
            "+491511111111",
            "SMS-FAILED",
            TestContext.Current.CancellationToken,
            new ChannelDispatchContext { CorrelationKey = "sms-claim-1" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SMS send failed.");

        var audit = await db.Set<ChannelDispatchAudit>()
            .SingleAsync(x => x.CorrelationKey == "sms-claim-1", TestContext.Current.CancellationToken);
        audit.Channel.Should().Be("SMS");
        audit.Provider.Should().Be("Twilio");
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().NotBeNullOrWhiteSpace();
        audit.AttemptedAtUtc.Should().Be(clock.UtcNow);
        audit.CompletedAtUtc.Should().NotBeNull();
        capturedRequest.Requests.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Sent")]
    public async Task ProviderBackedSmsSender_Should_SkipDuplicateSend_When_ActiveAudit_AlreadyExists(string status)
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<SiteSetting>().Add(
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Darwin",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "ops@darwin.de",
                HomeSlug = "home",
                SmsEnabled = true,
                SmsProvider = "Twilio",
                SmsApiKey = "active-api-key",
                SmsApiSecret = "active-api-secret",
                SmsFromPhoneE164 = "+4915000000000"
            });
        db.Set<ChannelDispatchAudit>().Add(new ChannelDispatchAudit
        {
            Id = Guid.NewGuid(),
            Channel = "SMS",
            Provider = "Twilio",
            CorrelationKey = "sms-retry-1",
            RecipientAddress = "+491511111111",
            IntendedRecipientAddress = "+491511111111",
            MessagePreview = "already sent",
            Status = status,
            AttemptedAtUtc = new DateTime(2030, 1, 1, 11, 55, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var capturedRequest = new CapturingHttpMessageHandler();
        capturedRequest.Configure((_, _) => throw new InvalidOperationException("Should not be called"));
        var sender = new ProviderBackedSmsSender(
            db,
            new FakeHttpClientFactory(capturedRequest),
            NullLogger<ProviderBackedSmsSender>.Instance,
            new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        var act = () => sender.SendAsync(
            "+491511111111",
            "SMS-DUP",
            TestContext.Current.CancellationToken,
            new ChannelDispatchContext { CorrelationKey = "sms-retry-1" });

        await act.Should().NotThrowAsync();

        capturedRequest.Requests.Should().BeEmpty();
        (await db.Set<ChannelDispatchAudit>().CountAsync(TestContext.Current.CancellationToken))
            .Should().Be(1);
    }

    [Fact]
    public async Task ProviderBackedSmsSender_Should_AllowRetry_WithNewCorrelation_AfterFailedOrOldPendingAudits()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<SiteSetting>().Add(
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Darwin",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "ops@darwin.de",
                HomeSlug = "home",
                SmsEnabled = true,
                SmsProvider = "Twilio",
                SmsApiKey = "active-api-key",
                SmsApiSecret = "active-api-secret",
                SmsFromPhoneE164 = "+4915000000000"
            });
        db.Set<ChannelDispatchAudit>().AddRange(
            new ChannelDispatchAudit
            {
                Id = Guid.NewGuid(),
                Channel = "SMS",
                Provider = "Twilio",
                CorrelationKey = "sms-old-failed",
                RecipientAddress = "+491511111111",
                IntendedRecipientAddress = "+491511111111",
                MessagePreview = "old failed",
                Status = "Failed",
                AttemptedAtUtc = new DateTime(2030, 1, 1, 11, 10, 0, DateTimeKind.Utc)
            },
            new ChannelDispatchAudit
            {
                Id = Guid.NewGuid(),
                Channel = "SMS",
                Provider = "Twilio",
                CorrelationKey = "sms-old-pending",
                RecipientAddress = "+491511111112",
                IntendedRecipientAddress = "+491511111112",
                MessagePreview = "old pending",
                Status = "Pending",
                AttemptedAtUtc = new DateTime(2030, 1, 1, 10, 10, 0, DateTimeKind.Utc)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var capturedRequest = new CapturingHttpMessageHandler();
        capturedRequest.Configure(
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"sid":"SM_RETRY"}""", Encoding.UTF8, "application/json")
            });

        var sender = new ProviderBackedSmsSender(
            db,
            new FakeHttpClientFactory(capturedRequest),
            NullLogger<ProviderBackedSmsSender>.Instance,
            new TestClock(new DateTime(2030, 1, 1, 12, 5, 0, DateTimeKind.Utc)));

        await sender.SendAsync(
            "+491511111113",
            "SMS-RETRY",
            TestContext.Current.CancellationToken,
            new ChannelDispatchContext { CorrelationKey = "sms-new-retry" });

        capturedRequest.Requests.Should().HaveCount(1);
        var retryAudit = await db.Set<ChannelDispatchAudit>()
            .SingleAsync(x => x.CorrelationKey == "sms-new-retry", TestContext.Current.CancellationToken);
        retryAudit.Status.Should().Be("Sent");
        retryAudit.ProviderMessageId.Should().Be("SM_RETRY");

        (await db.Set<ChannelDispatchAudit>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
    }

    [Fact]
    public async Task MetaWhatsAppSender_Should_CreateAuditClaim_BeforeTransportFailure()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<SiteSetting>().Add(
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Darwin",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "ops@darwin.de",
                HomeSlug = "home",
                WhatsAppEnabled = true,
                WhatsAppBusinessPhoneId = "phone-id",
                WhatsAppAccessToken = "active-token"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var capturedRequest = new CapturingHttpMessageHandler();
        capturedRequest.Configure(
            (_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("""{"error":{"message":"temporary"}}""", Encoding.UTF8, "application/json")
            });

        var clock = new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        var sender = new MetaWhatsAppSender(
            db,
            new FakeHttpClientFactory(capturedRequest),
            NullLogger<MetaWhatsAppSender>.Instance,
            clock);

        var act = () => sender.SendTextAsync(
            "+491511111111",
            "WA-FAILED",
            TestContext.Current.CancellationToken,
            new ChannelDispatchContext { CorrelationKey = "wa-claim-1" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("WhatsApp send failed.");

        var audit = await db.Set<ChannelDispatchAudit>()
            .SingleAsync(x => x.CorrelationKey == "wa-claim-1", TestContext.Current.CancellationToken);
        audit.Channel.Should().Be("WhatsApp");
        audit.Provider.Should().Be("Meta");
        audit.Status.Should().Be("Failed");
        audit.AttemptedAtUtc.Should().Be(clock.UtcNow);
        audit.CompletedAtUtc.Should().NotBeNull();
        capturedRequest.Requests.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Sent")]
    public async Task MetaWhatsAppSender_Should_SkipDuplicateSend_When_ActiveAudit_AlreadyExists(string status)
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<SiteSetting>().Add(
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Darwin",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "ops@darwin.de",
                HomeSlug = "home",
                WhatsAppEnabled = true,
                WhatsAppBusinessPhoneId = "phone-id",
                WhatsAppAccessToken = "active-token"
            });
        db.Set<ChannelDispatchAudit>().Add(new ChannelDispatchAudit
        {
            Id = Guid.NewGuid(),
            Channel = "WhatsApp",
            Provider = "Meta",
            CorrelationKey = "wa-retry-1",
            RecipientAddress = "+491511111111",
            IntendedRecipientAddress = "+491511111111",
            MessagePreview = "already sent",
            Status = status,
            AttemptedAtUtc = new DateTime(2030, 1, 1, 11, 55, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var capturedRequest = new CapturingHttpMessageHandler();
        capturedRequest.Configure((_, _) => throw new InvalidOperationException("Should not be called"));
        var sender = new MetaWhatsAppSender(
            db,
            new FakeHttpClientFactory(capturedRequest),
            NullLogger<MetaWhatsAppSender>.Instance,
            new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        var act = () => sender.SendTextAsync(
            "+491511111111",
            "WA-DUP",
            TestContext.Current.CancellationToken,
            new ChannelDispatchContext { CorrelationKey = "wa-retry-1" });

        await act.Should().NotThrowAsync();

        capturedRequest.Requests.Should().BeEmpty();
        (await db.Set<ChannelDispatchAudit>().CountAsync(TestContext.Current.CancellationToken))
            .Should().Be(1);
    }

    [Fact]
    public async Task MetaWhatsAppSender_Should_AllowRetry_WithNewCorrelation_AfterFailedOrOldPendingAudits()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<SiteSetting>().Add(
            new SiteSetting
            {
                Id = Guid.NewGuid(),
                Title = "Darwin",
                DefaultCulture = "de-DE",
                SupportedCulturesCsv = "de-DE,en-US",
                ContactEmail = "ops@darwin.de",
                HomeSlug = "home",
                WhatsAppEnabled = true,
                WhatsAppBusinessPhoneId = "phone-id",
                WhatsAppAccessToken = "active-token"
            });
        db.Set<ChannelDispatchAudit>().AddRange(
            new ChannelDispatchAudit
            {
                Id = Guid.NewGuid(),
                Channel = "WhatsApp",
                Provider = "Meta",
                CorrelationKey = "wa-old-failed",
                RecipientAddress = "+491511111111",
                IntendedRecipientAddress = "+491511111111",
                MessagePreview = "old failed",
                Status = "Failed",
                AttemptedAtUtc = new DateTime(2030, 1, 1, 11, 10, 0, DateTimeKind.Utc)
            },
            new ChannelDispatchAudit
            {
                Id = Guid.NewGuid(),
                Channel = "WhatsApp",
                Provider = "Meta",
                CorrelationKey = "wa-old-pending",
                RecipientAddress = "+491511111112",
                IntendedRecipientAddress = "+491511111112",
                MessagePreview = "old pending",
                Status = "Pending",
                AttemptedAtUtc = new DateTime(2030, 1, 1, 10, 10, 0, DateTimeKind.Utc)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var capturedRequest = new CapturingHttpMessageHandler();
        capturedRequest.Configure(
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"messages":[{"id":"wamid.retry"}]}""", Encoding.UTF8, "application/json")
            });

        var sender = new MetaWhatsAppSender(
            db,
            new FakeHttpClientFactory(capturedRequest),
            NullLogger<MetaWhatsAppSender>.Instance,
            new TestClock(new DateTime(2030, 1, 1, 12, 5, 0, DateTimeKind.Utc)));

        await sender.SendTextAsync(
            "+491511111113",
            "WA-RETRY",
            TestContext.Current.CancellationToken,
            new ChannelDispatchContext { CorrelationKey = "wa-new-retry" });

        capturedRequest.Requests.Should().HaveCount(1);
        var retryAudit = await db.Set<ChannelDispatchAudit>()
            .SingleAsync(x => x.CorrelationKey == "wa-new-retry", TestContext.Current.CancellationToken);
        retryAudit.Status.Should().Be("Sent");
        retryAudit.ProviderMessageId.Should().Be("wamid.retry");

        (await db.Set<ChannelDispatchAudit>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
    }

    [Fact]
    public async Task SmtpEmailSender_Should_CreateAuditClaim_BeforeTransportFailure()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        var sender = new SmtpEmailSender(
            Options.Create(new SmtpEmailOptions
            {
                Host = "unreachable.localhost",
                Port = 2525,
                FromAddress = "noreply@darwin.de",
                FromDisplayName = "Darwin Notifications"
            }),
            NullLogger<SmtpEmailSender>.Instance,
            db,
            new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        var act = () => sender.SendAsync(
            "bad-email",
            "SMTP-Failure",
            "<p>Body</p>",
            TestContext.Current.CancellationToken,
            new EmailDispatchContext { CorrelationKey = "smtp-claim-1" });

        await act.Should().ThrowAsync<FormatException>();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.CorrelationKey == "smtp-claim-1", TestContext.Current.CancellationToken);
        audit.Provider.Should().Be("SMTP");
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SmtpEmailSender_Should_SkipDuplicateSend_When_ActiveAudit_AlreadyExists_ForCorrelation()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Id = Guid.NewGuid(),
            Provider = "SMTP",
            CorrelationKey = "smtp-retry-1",
            RecipientEmail = "ops@darwin.de",
            Subject = "preexisting",
            IntendedRecipientEmail = "ops@darwin.de",
            Status = "Sent",
            AttemptedAtUtc = new DateTime(2030, 1, 1, 11, 55, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sender = new SmtpEmailSender(
            Options.Create(new SmtpEmailOptions { Host = "unreachable.localhost", Port = 2525, FromAddress = "noreply@darwin.de" }),
            NullLogger<SmtpEmailSender>.Instance,
            db,
            new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        var act = () => sender.SendAsync(
            "user@darwin.de",
            "Retry blocked",
            "<p>Body</p>",
            TestContext.Current.CancellationToken,
            new EmailDispatchContext { CorrelationKey = "smtp-retry-1" });

        await act.Should().NotThrowAsync();

        (await db.Set<EmailDispatchAudit>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task SmtpEmailSender_Should_AllowRetry_WithNewCorrelation_AfterFailedOrOldPendingAudits()
    {
        await using var db = NotificationSenderTestDbContext.Create();

        db.Set<EmailDispatchAudit>().AddRange(
            new EmailDispatchAudit
            {
                Id = Guid.NewGuid(),
                Provider = "SMTP",
                CorrelationKey = "smtp-old-failed",
                RecipientEmail = "user@darwin.de",
                Subject = "failed",
                IntendedRecipientEmail = "user@darwin.de",
                Status = "Failed",
                AttemptedAtUtc = new DateTime(2030, 1, 1, 11, 10, 0, DateTimeKind.Utc)
            },
            new EmailDispatchAudit
            {
                Id = Guid.NewGuid(),
                Provider = "SMTP",
                CorrelationKey = "smtp-old-pending",
                RecipientEmail = "user2@darwin.de",
                Subject = "old pending",
                IntendedRecipientEmail = "user2@darwin.de",
                Status = "Pending",
                AttemptedAtUtc = new DateTime(2030, 1, 1, 10, 10, 0, DateTimeKind.Utc)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sender = new SmtpEmailSender(
            Options.Create(new SmtpEmailOptions
            {
                Host = "unreachable.localhost",
                Port = 2525,
                FromAddress = "noreply@darwin.de",
                FromDisplayName = "Darwin Notifications"
            }),
            NullLogger<SmtpEmailSender>.Instance,
            db,
            new TestClock(new DateTime(2030, 1, 1, 12, 5, 0, DateTimeKind.Utc)));

        var act = () => sender.SendAsync(
            "bad-email",
            "SMTP-RETRY",
            "<p>Body</p>",
            TestContext.Current.CancellationToken,
            new EmailDispatchContext { CorrelationKey = "smtp-new-retry" });

        await act.Should().ThrowAsync<FormatException>();

        (await db.Set<EmailDispatchAudit>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
        var retryAudit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.CorrelationKey == "smtp-new-retry", TestContext.Current.CancellationToken);
        retryAudit.Status.Should().Be("Failed");
    }

    private sealed class TestClock : IClock
    {
        public TestClock(DateTime utcNow)
            => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }

    private sealed class NotificationSenderTestDbContext : DbContext, IAppDbContext
    {
        private NotificationSenderTestDbContext(DbContextOptions<NotificationSenderTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static NotificationSenderTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<NotificationSenderTestDbContext>()
                .UseInMemoryDatabase($"darwin_notification_sender_tests_{Guid.NewGuid()}")
                .Options;
            return new NotificationSenderTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SiteSetting>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.DefaultCulture).IsRequired();
                b.Property(x => x.SupportedCulturesCsv).IsRequired();
                b.Property(x => x.ContactEmail).IsRequired();
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<EmailDispatchAudit>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.RecipientEmail).IsRequired();
                b.Property(x => x.Subject).IsRequired();
                b.Property(x => x.Status).IsRequired();
            });

            modelBuilder.Entity<ChannelDispatchAudit>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Channel).IsRequired();
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.RecipientAddress).IsRequired();
                b.Property(x => x.MessagePreview).IsRequired();
                b.Property(x => x.Status).IsRequired();
            });
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name)
            => new(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://localhost.localdomain")
            };
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage?, CancellationToken, HttpResponseMessage> Resolver { get; private set; } =
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK);
        public List<HttpRequestMessage> Requests { get; } = new();

        public void Configure(Func<HttpRequestMessage?, CancellationToken, HttpResponseMessage> resolver) =>
            Resolver = resolver;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(Resolver(request, ct));
        }
    }
}
