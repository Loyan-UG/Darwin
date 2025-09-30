using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications.Smtp
{
    /// <summary>
    ///     SMTP-based implementation of <see cref="IEmailSender"/>.
    ///     Uses <see cref="SmtpClient"/> under the hood with configuration from <see cref="SmtpEmailOptions"/>.
    ///     Keep HTML content sanitized at the Application layer (do not trust inputs here).
    /// </summary>
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpEmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        /// <summary>
        ///     Creates an instance of the SMTP email sender.
        /// </summary>
        /// <param name="options">Bound configuration for SMTP host, port, credentials and defaults.</param>
        /// <param name="logger">Logger for operational diagnostics.</param>
        public SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Sends an HTML email using the configured SMTP relay.
        /// </summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="subject">Email subject line (plain text).</param>
        /// <param name="htmlBody">Email body in HTML format.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) throw new ArgumentNullException(nameof(toEmail));
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
                Subject = subject ?? string.Empty,
                Body = htmlBody ?? string.Empty,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_options.Username))
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);

            // SmtpClient has no truly async send on .NET (it provides SendMailAsync)
            await client.SendMailAsync(message);
            _logger.LogInformation("SMTP email sent to {Recipient} via {Host}:{Port}", toEmail, _options.Host, _options.Port);
        }
    }
}
