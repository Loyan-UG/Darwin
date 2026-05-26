using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Notifications.Brevo;
using Darwin.Infrastructure.Notifications.Smtp;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Notifications;

/// <summary>
/// Runtime email sender that selects the active transactional provider from Site Settings.
/// </summary>
public sealed class SiteSettingEmailSender : IEmailSender
{
    private readonly IAppDbContext _db;
    private readonly BrevoEmailSender _brevo;
    private readonly SmtpEmailSender _smtp;

    public SiteSettingEmailSender(IAppDbContext db, BrevoEmailSender brevo, SmtpEmailSender smtp)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _brevo = brevo ?? throw new ArgumentNullException(nameof(brevo));
        _smtp = smtp ?? throw new ArgumentNullException(nameof(smtp));
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default,
        EmailDispatchContext? context = null)
    {
        var settings = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted, ct)
            .ConfigureAwait(false);

        var provider = string.IsNullOrWhiteSpace(settings?.TransactionalEmailProvider)
            ? EmailProviderNames.Brevo
            : EmailProviderNames.Normalize(settings.TransactionalEmailProvider);

        var senderContext = context ?? new EmailDispatchContext();
        senderContext.SenderRole = EmailSenderIdentityResolver.NormalizeRole(senderContext.SenderRole);

        if (string.Equals(provider, EmailProviderNames.Brevo, StringComparison.OrdinalIgnoreCase))
        {
            await _brevo.SendAsync(toEmail, subject, htmlBody, ct, senderContext).ConfigureAwait(false);
            return;
        }

        if (string.Equals(provider, EmailProviderNames.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            await _smtp.SendAsync(toEmail, subject, htmlBody, ct, senderContext).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"Unsupported email provider '{provider}'.");
    }
}
