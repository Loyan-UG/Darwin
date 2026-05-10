using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Services;
using Darwin.Infrastructure.Notifications;
using Darwin.Infrastructure.Notifications.Brevo;
using Darwin.Infrastructure.Notifications.BusinessInvitations;
using Darwin.Infrastructure.Notifications.InactiveReminders;
using Darwin.Infrastructure.Notifications.Sms;
using Darwin.Infrastructure.Notifications.Smtp;
using Darwin.Infrastructure.Notifications.WhatsApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Extensions
{
    /// <summary>
    ///     Composition root for notifications infrastructure.
    ///     Call <c>services.AddNotificationsInfrastructure(configuration)</c> once from Web composition.
    /// </summary>
    public static class ServiceCollectionExtensionsNotifications
    {
        /// <summary>
        ///     Registers transactional email senders and binds options from configuration section "Email".
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Application configuration (appsettings).</param>
        public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IValidateOptions<EmailDeliveryOptions>, EmailDeliveryOptionsValidator>();
            services.AddOptions<EmailDeliveryOptions>()
                .Bind(configuration.GetSection("Email"))
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<SmtpEmailOptions>, SmtpEmailOptionsValidator>();
            services.AddOptions<SmtpEmailOptions>()
                .Bind(configuration.GetSection("Email:Smtp"))
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<BrevoEmailOptions>, BrevoEmailOptionsValidator>();
            services.AddOptions<BrevoEmailOptions>()
                .Bind(configuration.GetSection("Email:Brevo"))
                .ValidateOnStart();
            services.Configure<BusinessInvitationLinkOptions>(configuration.GetSection("BusinessOnboarding:InvitationMagicLink"));
            services.Configure<InactiveReminderPushGatewayOptions>(configuration.GetSection("Notifications:InactiveReminderPushGateway"));

            services.AddScoped<SmtpEmailSender>();
            services.AddHttpClient<BrevoEmailSender>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<BrevoEmailOptions>>().Value;
                var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl) ? "https://api.brevo.com/v3/" : options.BaseUrl.Trim();
                client.BaseAddress = new Uri(baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/");
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
            });
            services.AddScoped<IEmailSender>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailDeliveryOptions>>().Value;
                return EmailProviderNames.Normalize(options.Provider) switch
                {
                    EmailProviderNames.Brevo => serviceProvider.GetRequiredService<BrevoEmailSender>(),
                    EmailProviderNames.Smtp => serviceProvider.GetRequiredService<SmtpEmailSender>(),
                    var provider => throw new InvalidOperationException($"Unsupported email provider '{provider}'.")
                };
            });
            services.AddHttpClient(nameof(ProviderBackedSmsSender));
            services.AddScoped<ProviderBackedSmsSender>();
            services.AddScoped<ISmsSender, ProviderBackedSmsSender>();
            services.AddHttpClient(nameof(MetaWhatsAppSender));
            services.AddScoped<MetaWhatsAppSender>();
            services.AddScoped<IWhatsAppSender, MetaWhatsAppSender>();
            services.AddSingleton<IBusinessInvitationLinkBuilder, ConfigBusinessInvitationLinkBuilder>();
            services.AddHttpClient<HttpInactiveReminderDispatcher>();
            services.AddScoped<IInactiveReminderDispatcher, HttpInactiveReminderDispatcher>();
            return services;
        }
    }
}
