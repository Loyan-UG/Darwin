using Darwin.Application.Abstractions.Notifications;
using Darwin.Infrastructure.Notifications.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions
{
    /// <summary>
    ///     Composition root for notifications infrastructure (SMTP).
    ///     Call <c>services.AddNotificationsInfrastructure(configuration)</c> once from Web composition.
    /// </summary>
    public static class ServiceCollectionExtensions_Notifications
    {
        /// <summary>
        ///     Registers SMTP email sender and binds options from configuration section "Email:Smtp".
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Application configuration (appsettings).</param>
        public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SmtpEmailOptions>(configuration.GetSection("Email:Smtp"));
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            return services;
        }
    }
}
