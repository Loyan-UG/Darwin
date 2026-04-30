using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Notifications.Sms;

public sealed class ProviderBackedSmsSender : ISmsSender
{
    private readonly IAppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProviderBackedSmsSender> _logger;
    private readonly IClock _clock;

    public ProviderBackedSmsSender(
        IAppDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<ProviderBackedSmsSender> logger,
        IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task SendAsync(
        string toPhoneE164,
        string text,
        CancellationToken ct = default,
        ChannelDispatchContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(toPhoneE164)) throw new ArgumentNullException(nameof(toPhoneE164));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));
        var correlationKey = NormalizeCorrelationKey(context?.CorrelationKey);
        var pendingDuplicateCutoffUtc = _clock.UtcNow.AddMinutes(-15);
        if (!string.IsNullOrWhiteSpace(correlationKey) &&
            await _db.Set<ChannelDispatchAudit>()
                .AsNoTracking()
                .AnyAsync(
                    x => !x.IsDeleted &&
                         x.CorrelationKey == correlationKey &&
                         (x.Status == "Sent" || (x.Status == "Pending" && x.AttemptedAtUtc >= pendingDuplicateCutoffUtc)),
                    ct)
                .ConfigureAwait(false))
        {
            _logger.LogInformation("Skipping duplicate SMS send for correlation {CorrelationKey}.", correlationKey);
            return;
        }

        var settings = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted, ct);
        if (settings is null || !settings.SmsEnabled)
        {
            throw new InvalidOperationException("SMS transport is disabled.");
        }

        if (!string.Equals(settings.SmsProvider, "Twilio", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only the Twilio SMS provider is currently implemented.");
        }

        if (string.IsNullOrWhiteSpace(settings.SmsApiKey) ||
            string.IsNullOrWhiteSpace(settings.SmsApiSecret) ||
            string.IsNullOrWhiteSpace(settings.SmsFromPhoneE164))
        {
            throw new InvalidOperationException("Twilio SMS settings are incomplete.");
        }

        var attemptedAtUtc = _clock.UtcNow;
        var audit = new ChannelDispatchAudit
        {
            Channel = "SMS",
            Provider = settings.SmsProvider!.Trim(),
            FlowKey = string.IsNullOrWhiteSpace(context?.FlowKey) ? null : context.FlowKey.Trim(),
            TemplateKey = string.IsNullOrWhiteSpace(context?.TemplateKey) ? null : context.TemplateKey.Trim(),
            CorrelationKey = correlationKey,
            BusinessId = context?.BusinessId,
            RecipientAddress = toPhoneE164,
            IntendedRecipientAddress = string.IsNullOrWhiteSpace(context?.IntendedRecipientAddress) ? toPhoneE164 : context.IntendedRecipientAddress.Trim(),
            MessagePreview = BuildPreview(text),
            Status = "Pending",
            AttemptedAtUtc = attemptedAtUtc,
            CreatedAtUtc = attemptedAtUtc
        };
        _db.Set<ChannelDispatchAudit>().Add(audit);
        try
        {
            await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "SMS dispatch audit claim", ct)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(correlationKey) && !ct.IsCancellationRequested)
        {
            _db.Set<ChannelDispatchAudit>().Remove(audit);
            if (await HasActiveChannelAuditAsync("SMS", correlationKey, pendingDuplicateCutoffUtc, ct).ConfigureAwait(false))
            {
                _logger.LogInformation("Skipping duplicate SMS send for correlation {CorrelationKey}.", correlationKey);
                return;
            }

            throw;
        }

        var accountSid = settings.SmsApiKey.Trim();
        var authToken = settings.SmsApiSecret.Trim();
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(ProviderBackedSmsSender));
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["To"] = toPhoneE164,
                    ["From"] = settings.SmsFromPhoneE164.Trim(),
                    ["Body"] = text
                })
            };
            var authBytes = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(authBytes));

            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var completedAtUtc = _clock.UtcNow;
                audit.Status = "Failed";
                audit.CompletedAtUtc = completedAtUtc;
                audit.FailureMessage = NotificationLogSanitizer.ProviderFailure("Twilio SMS", (int)response.StatusCode);
                await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "SMS dispatch audit provider failure", ct)
                    .ConfigureAwait(false);
                _logger.LogError(
                    "Twilio SMS send failed with status {StatusCode} for recipient {Recipient}.",
                    (int)response.StatusCode,
                    NotificationLogSanitizer.MaskPhone(toPhoneE164));
                throw new InvalidOperationException("SMS send failed.");
            }

            audit.ProviderMessageId = ExtractProviderMessageId(body);
            audit.Status = "Sent";
            audit.CompletedAtUtc = _clock.UtcNow;
            await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "SMS dispatch audit completion", ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (audit.Status == "Pending")
        {
            var completedAtUtc = _clock.UtcNow;
            audit.Status = "Failed";
            audit.CompletedAtUtc = completedAtUtc;
            audit.FailureMessage = NotificationLogSanitizer.TransportFailure("Twilio SMS");
            await NotificationAuditSaveResilience.SaveAsync(_db, _logger, "SMS dispatch audit transport failure", ct)
                .ConfigureAwait(false);
            _logger.LogError(ex, "Twilio SMS send failed before receiving a provider response.");
            throw;
        }
    }

    private static string BuildPreview(string text)
    {
        var value = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        return value.Length <= 240 ? value : value[..240];
    }

    private static string? ExtractProviderMessageId(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("sid", out var sidElement))
            {
                var sid = sidElement.GetString();
                return string.IsNullOrWhiteSpace(sid) ? null : sid.Trim();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string? NormalizeCorrelationKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private Task<bool> HasActiveChannelAuditAsync(
        string channel,
        string correlationKey,
        DateTime pendingDuplicateCutoffUtc,
        CancellationToken ct)
    {
        return _db.Set<ChannelDispatchAudit>()
            .AsNoTracking()
            .AnyAsync(
                x => !x.IsDeleted &&
                     x.Channel == channel &&
                     x.CorrelationKey == correlationKey &&
                     (x.Status == "Sent" || (x.Status == "Pending" && x.AttemptedAtUtc >= pendingDuplicateCutoffUtc)),
                ct);
    }
}
