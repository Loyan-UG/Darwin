using System;
using System.Linq;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Infrastructure.Extensions;
using Darwin.Infrastructure.Notifications;
using Darwin.Infrastructure.Notifications.Brevo;
using Darwin.Infrastructure.Notifications.Smtp;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Tests.Notifications;

public sealed class ServiceCollectionExtensionsNotificationsTests
{
    [Fact]
    public void AddNotificationsInfrastructure_Should_Resolve_IEmailSender_AsBrevoSender_WhenProviderIsBrevo()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            ("Email:Provider", EmailProviderNames.Brevo),
            ("Email:Brevo:ApiKey", "brevo-api-key"),
            ("Email:Brevo:SenderEmail", "noreply@darwin.de"),
            ("Email:Brevo:WebhookUsername", "brevo-user"),
            ("Email:Brevo:WebhookPassword", "brevo-pass"),
            ("Email:Brevo:TimeoutSeconds", "20"));

        AddNotificationDependencies(services);
        services.AddNotificationsInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        sender.Should().BeOfType<BrevoEmailSender>();
    }

    [Fact]
    public void AddNotificationsInfrastructure_Should_Resolve_IEmailSender_AsSmtpSender_WhenProviderIsSmtp()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(("Email:Provider", EmailProviderNames.Smtp));

        AddNotificationDependencies(services);
        services.AddNotificationsInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        sender.Should().BeOfType<SmtpEmailSender>();
    }

    [Fact]
    public void AddNotificationsInfrastructure_Should_Throw_WhenUnsupportedEmailProviderIsConfigured()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(("Email:Provider", "Postmark"));

        AddNotificationDependencies(services);
        services.AddNotificationsInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var action = () => scope.ServiceProvider.GetRequiredService<IEmailSender>();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Unsupported email provider 'Postmark'.");
    }

    [Fact]
    public void AddNotificationsInfrastructure_Should_FailStartup_WhenBrevoIsConfiguredWithoutRequiredBrevoOptions()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            ("Email:Provider", EmailProviderNames.Brevo),
            ("Email:Brevo:SenderEmail", "noreply@darwin.de"),
            ("Email:Brevo:WebhookUsername", "brevo-user"),
            ("Email:Brevo:WebhookPassword", "brevo-pass"));

        AddNotificationDependencies(services);
        services.AddNotificationsInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var action = () => scope.ServiceProvider.GetRequiredService<IOptions<BrevoEmailOptions>>().Value;
        action.Should().Throw<OptionsValidationException>()
            .WithMessage("*Email:Brevo:ApiKey is required when Email:Provider is Brevo.*");
    }

    private static void AddNotificationDependencies(IServiceCollection services)
    {
        services.AddScoped<IClock>(static _ => new TestClock(new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        services.AddDbContext<NotificationServiceCollectionTestDbContext>(options =>
            options.UseInMemoryDatabase($"darwin_notifications_services_{Guid.NewGuid():N}"));
        services.AddScoped<IAppDbContext>(sp =>
            sp.GetRequiredService<NotificationServiceCollectionTestDbContext>());
    }

    private static IConfiguration BuildConfiguration(params (string key, string value)[] values)
    {
        var dictionary = values
            .Select(item => new KeyValuePair<string, string?>(item.key, item.value))
            .ToDictionary();
        return new ConfigurationBuilder().AddInMemoryCollection(dictionary).Build();
    }

    private sealed class TestClock : IClock
    {
        public TestClock(DateTime utcNow)
            => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }

    private sealed class NotificationServiceCollectionTestDbContext : DbContext, IAppDbContext
    {
        private NotificationServiceCollectionTestDbContext(DbContextOptions<NotificationServiceCollectionTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();
    }
}
