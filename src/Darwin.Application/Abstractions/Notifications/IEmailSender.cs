using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Notifications
{
    /// <summary>
    /// Minimal email sender abstraction used for password reset and admin notifications.
    /// Implementation may be SMTP or a 3rd party API, configured in Infrastructure.
    /// </summary>
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    }
}
