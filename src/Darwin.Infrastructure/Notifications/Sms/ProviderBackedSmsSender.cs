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
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Notifications.Sms;

public sealed class ProviderBackedSmsSender : ISmsSender
{
    private readonly IAppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProviderBackedSmsSender> _logger;

    public ProviderBackedSmsSender(
        IAppDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<ProviderBackedSmsSender> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendAsync(
        string toPhoneE164,
        string text,
        CancellationToken ct = default,
        ChannelDispatchContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(toPhoneE164)) throw new ArgumentNullException(nameof(toPhoneE164));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));

        var settings = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct);
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

        var attemptedAtUtc = DateTime.UtcNow;
        var audit = new ChannelDispatchAudit
        {
            Channel = "SMS",
            Provider = settings.SmsProvider!.Trim(),
            FlowKey = string.IsNullOrWhiteSpace(context?.FlowKey) ? null : context.FlowKey.Trim(),
            TemplateKey = string.IsNullOrWhiteSpace(context?.TemplateKey) ? null : context.TemplateKey.Trim(),
            CorrelationKey = string.IsNullOrWhiteSpace(context?.CorrelationKey) ? null : context.CorrelationKey.Trim(),
            BusinessId = context?.BusinessId,
            RecipientAddress = toPhoneE164,
            IntendedRecipientAddress = string.IsNullOrWhiteSpace(context?.IntendedRecipientAddress) ? toPhoneE164 : context.IntendedRecipientAddress.Trim(),
            MessagePreview = BuildPreview(text),
            Status = "Pending",
            AttemptedAtUtc = attemptedAtUtc,
            CreatedAtUtc = attemptedAtUtc
        };
        _db.Set<ChannelDispatchAudit>().Add(audit);

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
                audit.Status = "Failed";
                audit.CompletedAtUtc = DateTime.UtcNow;
                audit.FailureMessage = BuildFailure(body);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogError("Twilio SMS send failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
                throw new InvalidOperationException("SMS send failed.");
            }

            audit.ProviderMessageId = ExtractProviderMessageId(body);
            audit.Status = "Sent";
            audit.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (audit.Status == "Pending")
        {
            audit.Status = "Failed";
            audit.CompletedAtUtc = DateTime.UtcNow;
            audit.FailureMessage = BuildFailure(ex.Message);
            await _db.SaveChangesAsync(ct);
            _logger.LogError(ex, "Twilio SMS send failed before receiving a provider response.");
            throw;
        }
    }

    private static string BuildPreview(string text)
    {
        var value = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        return value.Length <= 240 ? value : value[..240];
    }

    private static string BuildFailure(string body)
    {
        var value = string.IsNullOrWhiteSpace(body) ? "SMS send failed." : body.Trim();
        return value.Length <= 2000 ? value : value[..2000];
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
}
