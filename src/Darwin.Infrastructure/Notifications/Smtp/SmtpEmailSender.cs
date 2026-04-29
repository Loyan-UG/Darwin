using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Darwin.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
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
        private readonly IAppDbContext _db;

        /// <summary>
        ///     Creates an instance of the SMTP email sender.
        /// </summary>
        /// <param name="options">Bound configuration for SMTP host, port, credentials and defaults.</param>
        /// <param name="logger">Logger for operational diagnostics.</param>
        public SmtpEmailSender(
            IOptions<SmtpEmailOptions> options,
            ILogger<SmtpEmailSender> logger,
            IAppDbContext db)
        {
            _options = (options ?? throw new ArgumentNullException(nameof(options))).Value
                       ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        ///     Sends an HTML email using the configured SMTP relay.
        /// </summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="subject">Email subject line (plain text).</param>
        /// <param name="htmlBody">Email body in HTML format.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default,
            EmailDispatchContext? context = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) throw new ArgumentNullException(nameof(toEmail));
            var correlationKey = NormalizeCorrelationKey(context?.CorrelationKey);
            var pendingDuplicateCutoffUtc = DateTime.UtcNow.AddMinutes(-15);
            if (!string.IsNullOrWhiteSpace(correlationKey) &&
                await _db.Set<EmailDispatchAudit>()
                    .AsNoTracking()
                    .AnyAsync(
                        x => !x.IsDeleted &&
                             x.CorrelationKey == correlationKey &&
                             (x.Status == "Sent" || (x.Status == "Pending" && x.AttemptedAtUtc >= pendingDuplicateCutoffUtc)),
                        ct)
                    .ConfigureAwait(false))
            {
                _logger.LogInformation("Skipping duplicate SMTP email send for correlation {CorrelationKey}.", correlationKey);
                return;
            }

            var attemptedAtUtc = DateTime.UtcNow;
            var audit = new EmailDispatchAudit
            {
                Provider = "SMTP",
                FlowKey = string.IsNullOrWhiteSpace(context?.FlowKey) ? null : context.FlowKey.Trim(),
                TemplateKey = string.IsNullOrWhiteSpace(context?.TemplateKey) ? null : context.TemplateKey.Trim(),
                CorrelationKey = correlationKey,
                BusinessId = context?.BusinessId,
                RecipientEmail = toEmail,
                IntendedRecipientEmail = string.IsNullOrWhiteSpace(context?.IntendedRecipientEmail) ? toEmail : context.IntendedRecipientEmail.Trim(),
                Subject = subject ?? string.Empty,
                Status = "Pending",
                AttemptedAtUtc = attemptedAtUtc,
                CreatedAtUtc = attemptedAtUtc
            };
            _db.Set<EmailDispatchAudit>().Add(audit);
            try
            {
                await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "SMTP email dispatch audit claim", ct)
                    .ConfigureAwait(false);
            }
            catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(correlationKey) && !ct.IsCancellationRequested)
            {
                _db.Set<EmailDispatchAudit>().Remove(audit);
                if (await HasActiveEmailAuditAsync(correlationKey, pendingDuplicateCutoffUtc, ct).ConfigureAwait(false))
                {
                    _logger.LogInformation("Skipping duplicate SMTP email send for correlation {CorrelationKey}.", correlationKey);
                    return;
                }

                throw;
            }

            try
            {
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

                await client.SendMailAsync(message);
                audit.Status = "Sent";
                audit.CompletedAtUtc = DateTime.UtcNow;
                await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "SMTP email dispatch audit completion", ct)
                    .ConfigureAwait(false);
                _logger.LogInformation("SMTP email sent to {Recipient} via {Host}:{Port}", toEmail, _options.Host, _options.Port);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (audit.Status == "Pending")
            {
                audit.Status = "Failed";
                audit.CompletedAtUtc = DateTime.UtcNow;
                audit.FailureMessage = ex.Message.Length > 2000 ? ex.Message.Substring(0, 2000) : ex.Message;
                await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "SMTP email dispatch audit failure", ct)
                    .ConfigureAwait(false);
                throw;
            }
        }

        private static string? NormalizeCorrelationKey(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private Task<bool> HasActiveEmailAuditAsync(string correlationKey, DateTime pendingDuplicateCutoffUtc, CancellationToken ct)
        {
            return _db.Set<EmailDispatchAudit>()
                .AsNoTracking()
                .AnyAsync(
                    x => !x.IsDeleted &&
                         x.CorrelationKey == correlationKey &&
                         (x.Status == "Sent" || (x.Status == "Pending" && x.AttemptedAtUtc >= pendingDuplicateCutoffUtc)),
                    ct);
        }
    }
}
