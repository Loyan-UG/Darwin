using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Notifications.WhatsApp;

public sealed class MetaWhatsAppSender : IWhatsAppSender
{
    private readonly IAppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MetaWhatsAppSender> _logger;

    public MetaWhatsAppSender(
        IAppDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<MetaWhatsAppSender> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendTextAsync(
        string toPhoneE164,
        string text,
        CancellationToken ct = default,
        ChannelDispatchContext? context = null)
    {
        var settings = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct);
        if (settings is null || !settings.WhatsAppEnabled)
        {
            throw new InvalidOperationException("WhatsApp transport is disabled.");
        }

        if (string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) ||
            string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken))
        {
            throw new InvalidOperationException("WhatsApp transport settings are incomplete.");
        }

        var attemptedAtUtc = DateTime.UtcNow;
        var audit = new ChannelDispatchAudit
        {
            Channel = "WhatsApp",
            Provider = "Meta",
            FlowKey = string.IsNullOrWhiteSpace(context?.FlowKey) ? null : context.FlowKey.Trim(),
            BusinessId = context?.BusinessId,
            RecipientAddress = toPhoneE164,
            MessagePreview = BuildPreview(text),
            Status = "Pending",
            AttemptedAtUtc = attemptedAtUtc,
            CreatedAtUtc = attemptedAtUtc
        };
        _db.Set<ChannelDispatchAudit>().Add(audit);

        var client = _httpClientFactory.CreateClient(nameof(MetaWhatsAppSender));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.WhatsAppAccessToken.Trim());

        var payload = new
        {
            messaging_product = "whatsapp",
            to = NormalizeRecipient(toPhoneE164),
            type = "text",
            text = new
            {
                preview_url = false,
                body = text
            }
        };

        using var response = await client.PostAsJsonAsync(
            $"https://graph.facebook.com/v22.0/{settings.WhatsAppBusinessPhoneId.Trim()}/messages",
            payload,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            audit.Status = "Failed";
            audit.CompletedAtUtc = DateTime.UtcNow;
            audit.FailureMessage = BuildFailure(body);
            await _db.SaveChangesAsync(ct);
            _logger.LogError("WhatsApp send failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException("WhatsApp send failed.");
        }

        audit.Status = "Sent";
        audit.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static string NormalizeRecipient(string phoneE164)
    {
        return phoneE164.Trim().TrimStart('+');
    }

    private static string BuildPreview(string text)
    {
        var value = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
        return value.Length <= 240 ? value : value[..240];
    }

    private static string BuildFailure(string body)
    {
        var value = string.IsNullOrWhiteSpace(body) ? "WhatsApp send failed." : body.Trim();
        return value.Length <= 2000 ? value : value[..2000];
    }
}
