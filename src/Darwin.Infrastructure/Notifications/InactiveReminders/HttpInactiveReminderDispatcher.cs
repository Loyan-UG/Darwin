using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Services;
using Darwin.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Darwin.Infrastructure.Notifications.InactiveReminders;

/// <summary>
/// Dispatches inactive reminders through a configurable HTTP push gateway endpoint.
/// </summary>
public sealed class HttpInactiveReminderDispatcher : IInactiveReminderDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<InactiveReminderPushGatewayOptions> _optionsMonitor;
    private readonly IClock _clock;
    private readonly ILogger<HttpInactiveReminderDispatcher> _logger;

    public HttpInactiveReminderDispatcher(
        HttpClient httpClient,
        IOptionsMonitor<InactiveReminderPushGatewayOptions> optionsMonitor,
        IClock clock,
        ILogger<HttpInactiveReminderDispatcher> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> DispatchAsync(
        Guid userId,
        string destinationDeviceId,
        string pushToken,
        string platform,
        int inactiveDays,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(destinationDeviceId))
        {
            return Result.Fail("Validation.DestinationDeviceIdRequired");
        }

        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return Result.Fail("Validation.PushTokenRequired");
        }

        var options = _optionsMonitor.CurrentValue;
        if (!options.Enabled)
        {
            return Result.Fail("Gateway.Disabled");
        }

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return Result.Fail("Gateway.EndpointNotConfigured");
        }

        var requestPayload = new InactiveReminderPushGatewayRequest
        {
            UserId = userId,
            DeviceId = destinationDeviceId,
            PushToken = pushToken,
            Platform = string.IsNullOrWhiteSpace(platform) ? "Unknown" : platform,
            Provider = NormalizeProviderFromPlatform(platform),
            InactiveDays = Math.Max(0, inactiveDays),
            Title = ApplyTemplate(options.TitleTemplate, inactiveDays),
            Body = ApplyTemplate(options.BodyTemplate, inactiveDays),
            AndroidChannelId = options.AndroidChannelId,
            ApnsTopic = options.ApnsTopic,
            DeepLinkUrl = options.DeepLinkUrl,
            CollapseKey = options.CollapseKey,
            AnalyticsLabel = options.AnalyticsLabel
        };

        var maxAttempts = Math.Clamp(options.MaxAttempts, 1, 5);
        var initialBackoffMs = Math.Clamp(options.InitialBackoffMilliseconds, 100, 10_000);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var request = BuildGatewayRequest(options, requestPayload);
                using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return Result.Ok();
                }

                var statusCode = (int)response.StatusCode;
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogWarning(
                    "Inactive reminder gateway rejected dispatch. Attempt={Attempt}/{MaxAttempts}, StatusCode={StatusCode}, UserId={UserId}",
                    attempt,
                    maxAttempts,
                    statusCode,
                    userId);

                var providerFailureCode = MapProviderFailureCodeFromBody(responseBody);
                if (!string.IsNullOrWhiteSpace(providerFailureCode))
                {
                    return Result.Fail(providerFailureCode);
                }

                var mappedFailure = MapGatewayFailureCode(statusCode);
                if (!IsTransientGatewayStatusCode(statusCode) || attempt >= maxAttempts)
                {
                    return Result.Fail(mappedFailure);
                }

                await DelayBeforeRetryAsync(attempt, initialBackoffMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Inactive reminder gateway transport error. Attempt={Attempt}/{MaxAttempts}, UserId={UserId}",
                    attempt,
                    maxAttempts,
                    userId);

                if (attempt >= maxAttempts)
                {
                    return Result.Fail("Gateway.TransportError");
                }

                await DelayBeforeRetryAsync(attempt, initialBackoffMs, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Inactive reminder gateway request timed out. Attempt={Attempt}/{MaxAttempts}, UserId={UserId}",
                    attempt,
                    maxAttempts,
                    userId);

                if (attempt >= maxAttempts)
                {
                    return Result.Fail("Gateway.Timeout");
                }

                await DelayBeforeRetryAsync(attempt, initialBackoffMs, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inactive reminder gateway dispatch failed for UserId={UserId}.", userId);
                return Result.Fail("Gateway.TransportError");
            }
        }

        return Result.Fail("Gateway.TransportError");
    }

    /// <summary>
    /// Builds one gateway request instance. Requests are not reused across retries.
    /// </summary>
    private static HttpRequestMessage BuildGatewayRequest(InactiveReminderPushGatewayOptions options, InactiveReminderPushGatewayRequest payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint)
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(options.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken.Trim());
        }

        return request;
    }

    /// <summary>
    /// Waits before retrying transient gateway failures using exponential backoff with bounded jitter.
    /// </summary>
    private async Task DelayBeforeRetryAsync(int attempt, int initialBackoffMs, CancellationToken ct)
    {
        var exponential = initialBackoffMs * Math.Pow(2, Math.Max(0, attempt - 1));
        var bounded = Math.Min(15_000d, exponential);

        // Small deterministic jitter derived from current clock ticks to reduce synchronized retries.
        var jitter = Math.Abs(_clock.UtcNow.Ticks % 200);
        var delay = TimeSpan.FromMilliseconds(bounded + jitter);
        await Task.Delay(delay, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether gateway status code is safe to retry.
    /// </summary>
    private static bool IsTransientGatewayStatusCode(int statusCode)
        => statusCode == 408 || statusCode == 429 || (statusCode >= 500 && statusCode <= 599);

    /// <summary>
    /// Applies simple placeholder replacement in templates.
    /// </summary>
    private static string ApplyTemplate(string template, int inactiveDays)
    {
        var safeTemplate = string.IsNullOrWhiteSpace(template)
            ? string.Empty
            : template;

        return safeTemplate.Replace("{inactiveDays}", Math.Max(0, inactiveDays).ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps mobile platform text to a gateway/provider hint for native sender routing.
    /// </summary>
    private static string NormalizeProviderFromPlatform(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return "Unknown";
        }

        var normalized = platform.Trim();
        if (normalized.Equals("Android", StringComparison.OrdinalIgnoreCase))
        {
            return "Fcm";
        }

        if (normalized.Equals("iOS", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("MacCatalyst", StringComparison.OrdinalIgnoreCase))
        {
            return "Apns";
        }

        return normalized;
    }



    /// <summary>
    /// Attempts to read provider-native failure reason from gateway response body.
    /// Expected fields: code, reason, providerCode, providerReason.
    /// </summary>
    private static string? MapProviderFailureCodeFromBody(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            var rawCode = TryReadString(root, "providerCode")
                ?? TryReadString(root, "providerReason")
                ?? TryReadString(root, "code")
                ?? TryReadString(root, "reason");

            if (string.IsNullOrWhiteSpace(rawCode))
            {
                return null;
            }

            var rawProvider = TryReadString(root, "provider")
                ?? TryReadString(root, "vendor")
                ?? string.Empty;

            var normalizedProvider = NormalizeProviderName(rawProvider);
            var mapped = MapKnownProviderFailure(normalizedProvider, rawCode);
            if (!string.IsNullOrWhiteSpace(mapped))
            {
                return mapped;
            }

            var normalized = NormalizeProviderCode(rawCode);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(normalizedProvider)
                ? $"Gateway.Provider.{normalized}"
                : $"Gateway.Provider.{normalizedProvider}.{normalized}";
        }
        catch
        {
            return null;
        }
    }


    /// <summary>
    /// Normalizes provider name to a compact taxonomy segment.
    /// </summary>
    private static string NormalizeProviderName(string? rawProvider)
    {
        if (string.IsNullOrWhiteSpace(rawProvider))
        {
            return string.Empty;
        }

        var provider = rawProvider.Trim();
        if (provider.Equals("fcm", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("firebase", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("firebasecloudmessaging", StringComparison.OrdinalIgnoreCase))
        {
            return "Fcm";
        }

        if (provider.Equals("apns", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("apple", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("applepush", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("applepushnotificationservice", StringComparison.OrdinalIgnoreCase))
        {
            return "Apns";
        }

        return NormalizeProviderCode(provider);
    }

    /// <summary>
    /// Maps known provider-specific codes to stable canonical taxonomy values.
    /// </summary>
    private static string? MapKnownProviderFailure(string normalizedProvider, string rawCode)
    {
        var code = rawCode.Trim();

        if (string.Equals(normalizedProvider, "Fcm", StringComparison.Ordinal))
        {
            if (code.Equals("UNREGISTERED", StringComparison.OrdinalIgnoreCase)
                || code.Equals("registration-token-not-registered", StringComparison.OrdinalIgnoreCase)
                || code.Equals("notregistered", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.TokenUnregistered";
            }

            if (code.Equals("INVALID_ARGUMENT", StringComparison.OrdinalIgnoreCase)
                || code.Equals("invalid-registration-token", StringComparison.OrdinalIgnoreCase)
                || code.Equals("invalidargument", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.InvalidArgument";
            }

            if (code.Equals("SENDER_ID_MISMATCH", StringComparison.OrdinalIgnoreCase)
                || code.Equals("sender-id-mismatch", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.SenderIdMismatch";
            }

            if (code.Equals("QUOTA_EXCEEDED", StringComparison.OrdinalIgnoreCase)
                || code.Equals("quota-exceeded", StringComparison.OrdinalIgnoreCase)
                || code.Equals("messageratelimitexceeded", StringComparison.OrdinalIgnoreCase)
                || code.Equals("devicemessageratelimitexceeded", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.QuotaExceeded";
            }

            if (code.Equals("UNAVAILABLE", StringComparison.OrdinalIgnoreCase)
                || code.Equals("internal", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Fcm.ServiceUnavailable";
            }
        }

        if (string.Equals(normalizedProvider, "Apns", StringComparison.Ordinal))
        {
            if (code.Equals("BadDeviceToken", StringComparison.OrdinalIgnoreCase)
                || code.Equals("DeviceTokenNotForTopic", StringComparison.OrdinalIgnoreCase)
                || code.Equals("Unregistered", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Apns.TokenInvalid";
            }

            if (code.Equals("TopicDisallowed", StringComparison.OrdinalIgnoreCase)
                || code.Equals("MissingTopic", StringComparison.OrdinalIgnoreCase)
                || code.Equals("BadTopic", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Apns.TopicInvalid";
            }

            if (code.Equals("ExpiredProviderToken", StringComparison.OrdinalIgnoreCase)
                || code.Equals("InvalidProviderToken", StringComparison.OrdinalIgnoreCase)
                || code.Equals("MissingProviderToken", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Apns.AuthTokenInvalid";
            }

            if (code.Equals("TooManyRequests", StringComparison.OrdinalIgnoreCase)
                || code.Equals("TooManyProviderTokenUpdates", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Apns.RateLimited";
            }

            if (code.Equals("ServiceUnavailable", StringComparison.OrdinalIgnoreCase)
                || code.Equals("Shutdown", StringComparison.OrdinalIgnoreCase)
                || code.Equals("InternalServerError", StringComparison.OrdinalIgnoreCase))
            {
                return "Gateway.Provider.Apns.ServiceUnavailable";
            }
        }

        return null;
    }

    /// <summary>
    /// Reads a string field from a JSON object in a safe way.
    /// </summary>
    private static string? TryReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    /// <summary>
    /// Converts provider reason text to a compact taxonomy segment.
    /// </summary>
    private static string NormalizeProviderCode(string rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return string.Empty;
        }

        var trimmed = rawCode.Trim();
        var chars = trimmed.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// Maps gateway HTTP status to stable failure taxonomy codes.
    /// </summary>
    private static string MapGatewayFailureCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "Gateway.BadRequest",
            401 => "Gateway.Unauthorized",
            403 => "Gateway.Forbidden",
            404 => "Gateway.EndpointNotFound",
            408 => "Gateway.Timeout",
            409 => "Gateway.Conflict",
            429 => "Gateway.RateLimited",
            >= 500 and <= 599 => "Gateway.ServerError",
            _ => $"Gateway.Http{statusCode}"
        };
    }

    /// <summary>
    /// Outbound gateway payload for one push dispatch request.
    /// </summary>
    private sealed class InactiveReminderPushGatewayRequest
    {
        /// <summary>
        /// Internal Darwin user identifier tied to the reminder candidate.
        /// </summary>
        public Guid UserId { get; init; }

        /// <summary>
        /// Device registration identifier resolved from the engagement snapshot.
        /// </summary>
        public string DeviceId { get; init; } = string.Empty;

        /// <summary>
        /// Provider-issued push token for the target device.
        /// </summary>
        public string PushToken { get; init; } = string.Empty;

        /// <summary>
        /// Original Darwin platform label (for example Android, iOS, MacCatalyst).
        /// </summary>
        public string Platform { get; init; } = "Unknown";

        /// <summary>
        /// Normalized provider hint used by the downstream gateway for routing.
        /// </summary>
        public string Provider { get; init; } = "Unknown";

        /// <summary>
        /// Number of inactive days included in the reminder copy and analytics.
        /// </summary>
        public int InactiveDays { get; init; }

        /// <summary>
        /// Push notification title rendered from the configured template.
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Push notification body rendered from the configured template.
        /// </summary>
        public string Body { get; init; } = string.Empty;

        /// <summary>
        /// Optional Android channel id for FCM-native dispatch.
        /// </summary>
        public string? AndroidChannelId { get; init; }

        /// <summary>
        /// Optional APNs topic/bundle id for APNs-native dispatch.
        /// </summary>
        public string? ApnsTopic { get; init; }

        /// <summary>
        /// Optional deep link that the client should open from the reminder tap action.
        /// </summary>
        public string? DeepLinkUrl { get; init; }

        /// <summary>
        /// Optional collapse key used to merge repeated inactive reminders.
        /// </summary>
        public string? CollapseKey { get; init; }

        /// <summary>
        /// Optional analytics label forwarded to the downstream sender.
        /// </summary>
        public string? AnalyticsLabel { get; init; }
    }
}
