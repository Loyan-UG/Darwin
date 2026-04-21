using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Services;
using Darwin.Infrastructure.Notifications.BusinessInvitations;
using Darwin.Infrastructure.Notifications.InactiveReminders;
using Darwin.Infrastructure.Notifications.Sms;
using Darwin.Infrastructure.Notifications.Smtp;
using Darwin.Infrastructure.Notifications.WhatsApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions
{
    /// <summary>
    ///     Composition root for notifications infrastructure (SMTP).
    ///     Call <c>services.AddNotificationsInfrastructure(configuration)</c> once from Web composition.
    /// </summary>
    public static class ServiceCollectionExtensionsNotifications
    {
        /// <summary>
        ///     Registers SMTP email sender and binds options from configuration section "Email:Smtp".
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Application configuration (appsettings).</param>
        public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SmtpEmailOptions>(configuration.GetSection("Email:Smtp"));
            services.Configure<BusinessInvitationLinkOptions>(configuration.GetSection("BusinessOnboarding:InvitationMagicLink"));
            services.Configure<InactiveReminderPushGatewayOptions>(configuration.GetSection("Notifications:InactiveReminderPushGateway"));

            services.AddScoped<SmtpEmailSender>();
            services.AddScoped<IEmailSender, SmtpEmailSender>();
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
