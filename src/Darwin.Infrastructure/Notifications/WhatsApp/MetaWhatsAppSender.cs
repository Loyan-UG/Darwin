using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendTextAsync(
        string toPhoneE164,
        string text,
        CancellationToken ct = default,
        ChannelDispatchContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(toPhoneE164)) throw new ArgumentNullException(nameof(toPhoneE164));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));

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

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(MetaWhatsAppSender));
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/v22.0/{settings.WhatsAppBusinessPhoneId.Trim()}/messages")
            {
                Content = JsonContent.Create(new
                {
                    messaging_product = "whatsapp",
                    to = NormalizeRecipient(toPhoneE164),
                    type = "text",
                    text = new
                    {
                        preview_url = false,
                        body = text
                    }
                })
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.WhatsAppAccessToken.Trim());

            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                audit.Status = "Failed";
                audit.CompletedAtUtc = DateTime.UtcNow;
                audit.FailureMessage = BuildFailure(body);
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogError("WhatsApp send failed with status {StatusCode}: {Body}", (int)response.StatusCode, body);
                throw new InvalidOperationException("WhatsApp send failed.");
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
            _logger.LogError(ex, "WhatsApp send failed before receiving a provider response.");
            throw;
        }
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

    private static string? ExtractProviderMessageId(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("messages", out var messagesElement) &&
                messagesElement.ValueKind == JsonValueKind.Array &&
                messagesElement.GetArrayLength() > 0 &&
                messagesElement[0].TryGetProperty("id", out var idElement))
            {
                var id = idElement.GetString();
                return string.IsNullOrWhiteSpace(id) ? null : id.Trim();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
