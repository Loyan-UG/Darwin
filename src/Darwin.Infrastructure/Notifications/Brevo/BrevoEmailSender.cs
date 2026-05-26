using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications.Brevo;

/// <summary>
/// Brevo API implementation of transactional email delivery.
/// </summary>
public sealed class BrevoEmailSender : IEmailSender
{
    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private readonly HttpClient _httpClient;
    private readonly BrevoEmailOptions _options;
    private readonly ILogger<BrevoEmailSender> _logger;
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<BrevoEmailOptions> options,
        ILogger<BrevoEmailSender> logger,
        IAppDbContext db,
        IClock clock)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default,
        EmailDispatchContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) throw new ArgumentNullException(nameof(toEmail));
        var settings = await LoadSettingsAsync(ct).ConfigureAwait(false);
        ValidateSettings(settings);
        ConfigureClient(settings);
        var senderRole = EmailSenderIdentityResolver.NormalizeRole(context?.SenderRole ?? EmailSenderRole.NoReply);

        var correlationKey = NormalizeCorrelationKey(context?.CorrelationKey);
        var pendingDuplicateCutoffUtc = _clock.UtcNow.AddMinutes(-15);
        if (!string.IsNullOrWhiteSpace(correlationKey) &&
            await HasActiveEmailAuditAsync(correlationKey, pendingDuplicateCutoffUtc, ct).ConfigureAwait(false))
        {
            _logger.LogInformation("Skipping duplicate Brevo email send for correlation {CorrelationKey}.", correlationKey);
            return;
        }

        var attemptedAtUtc = _clock.UtcNow;
        var audit = new EmailDispatchAudit
        {
            Provider = EmailProviderNames.Brevo,
            FlowKey = string.IsNullOrWhiteSpace(context?.FlowKey) ? null : context.FlowKey.Trim(),
            TemplateKey = string.IsNullOrWhiteSpace(context?.TemplateKey) ? null : context.TemplateKey.Trim(),
            SenderRole = EmailSenderIdentityResolver.RoleName(senderRole),
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
            await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "Brevo email dispatch audit claim", ct)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(correlationKey) && !ct.IsCancellationRequested)
        {
            _db.Set<EmailDispatchAudit>().Remove(audit);
            if (await HasActiveEmailAuditAsync(correlationKey, pendingDuplicateCutoffUtc, ct).ConfigureAwait(false))
            {
                _logger.LogInformation("Skipping duplicate Brevo email send for correlation {CorrelationKey}.", correlationKey);
                return;
            }

            throw;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email")
            {
                Content = JsonContent.Create(BuildPayload(toEmail, subject ?? string.Empty, htmlBody, context, correlationKey, settings, senderRole))
            };
            request.Headers.Add("api-key", settings!.BrevoApiKey!.Trim());
            request.Headers.Accept.ParseAdd("application/json");

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(NotificationLogSanitizer.ProviderFailure(EmailProviderNames.Brevo, (int)response.StatusCode));
            }

            audit.ProviderMessageId = TryReadMessageId(responseBody);
            audit.Status = "Sent";
            audit.CompletedAtUtc = _clock.UtcNow;
            await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "Brevo email dispatch audit completion", ct)
                .ConfigureAwait(false);
            _logger.LogInformation(
                "Brevo email sent to {Recipient} with message id {MessageId}.",
                NotificationLogSanitizer.MaskEmail(toEmail),
                audit.ProviderMessageId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception) when (audit.Status == "Pending")
        {
            audit.Status = "Failed";
            audit.CompletedAtUtc = _clock.UtcNow;
            audit.FailureMessage = NotificationLogSanitizer.TransportFailure(EmailProviderNames.Brevo);
            await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "Brevo email dispatch audit failure", ct)
                .ConfigureAwait(false);
            throw;
        }
    }

    private object BuildPayload(
        string toEmail,
        string subject,
        string htmlBody,
        EmailDispatchContext? context,
        string? correlationKey,
        SiteSetting? settings,
        EmailSenderRole senderRole)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(correlationKey))
        {
            headers["Idempotency-Key"] = correlationKey;
            headers["X-Correlation-Key"] = correlationKey;
        }

        if (settings?.BrevoSandboxMode == true)
        {
            headers["X-Sib-Sandbox"] = "drop";
        }

        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new
            {
                email = EmailSenderIdentityResolver.ResolveFromEmail(settings, senderRole),
                name = EmailSenderIdentityResolver.DefaultDisplayName
            },
            ["to"] = new[] { new { email = toEmail.Trim() } },
            ["tags"] = BuildTags(context)
        };

        var templateId = ResolveTemplateId(context);
        if (templateId.HasValue)
        {
            payload["templateId"] = templateId.Value;
            payload["params"] = BuildTemplateParameters(context);
        }
        else
        {
            payload["subject"] = subject ?? string.Empty;
            payload["htmlContent"] = htmlBody ?? string.Empty;
            payload["textContent"] = HtmlToText(htmlBody);
        }

        var replyToEmail = TrimToNull(EmailSenderIdentityResolver.ResolveReplyToEmail(settings));
        if (replyToEmail is not null)
        {
            payload["replyTo"] = new { email = replyToEmail, name = EmailSenderIdentityResolver.DefaultDisplayName };
        }

        if (headers.Count > 0)
        {
            payload["headers"] = headers;
        }

        return payload;
    }

    private int? ResolveTemplateId(EmailDispatchContext? context)
    {
        if (_options.TemplateIds.Count == 0 || context is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(context.TemplateKey) &&
            _options.TemplateIds.TryGetValue(context.TemplateKey.Trim(), out var byTemplateKey))
        {
            return byTemplateKey;
        }

        if (!string.IsNullOrWhiteSpace(context.FlowKey) &&
            _options.TemplateIds.TryGetValue(context.FlowKey.Trim(), out var byFlowKey))
        {
            return byFlowKey;
        }

        return null;
    }

    private static Dictionary<string, string> BuildTemplateParameters(EmailDispatchContext? context)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in context?.TemplateParameters ?? new Dictionary<string, string?>())
        {
            if (!string.IsNullOrWhiteSpace(pair.Key))
            {
                values[pair.Key.Trim()] = pair.Value ?? string.Empty;
            }
        }

        return values;
    }

    private string[] BuildTags(EmailDispatchContext? context)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in _options.DefaultTags ?? [])
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                tags.Add(tag.Trim());
            }
        }

        AddTag(tags, context?.FlowKey);
        AddTag(tags, context?.TemplateKey);
        return tags.ToArray();
    }

    private static void AddTag(HashSet<string> tags, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var tag = value.Trim().ToLowerInvariant().Replace(' ', '-');
        if (tag.Length > 0)
        {
            tags.Add(tag);
        }
    }

    private async Task<SiteSetting?> LoadSettingsAsync(CancellationToken ct)
    {
        return await _db.Set<SiteSetting>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted, ct)
            .ConfigureAwait(false);
    }

    private void ConfigureClient(SiteSetting? settings)
    {
        var baseUrl = string.IsNullOrWhiteSpace(settings?.BrevoBaseUrl)
            ? _options.BaseUrl
            : settings.BrevoBaseUrl.Trim();
        _httpClient.BaseAddress = new Uri(baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/");
    }

    private static void ValidateSettings(SiteSetting? settings)
    {
        if (string.IsNullOrWhiteSpace(settings?.BrevoApiKey))
        {
            throw new InvalidOperationException("Brevo API key is not configured in Site Settings.");
        }

        if (string.IsNullOrWhiteSpace(settings.NoReplyEmail))
        {
            throw new InvalidOperationException("No-reply sender email is not configured in Site Settings.");
        }
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

    private static string? TryReadMessageId(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.TryGetProperty("messageId", out var messageId)
                ? messageId.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string HtmlToText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return System.Net.WebUtility.HtmlDecode(HtmlTagRegex.Replace(html, " ")).Trim();
    }

    private static string? NormalizeCorrelationKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
