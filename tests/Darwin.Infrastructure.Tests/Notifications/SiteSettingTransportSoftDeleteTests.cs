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
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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
            (request, _) =>
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
